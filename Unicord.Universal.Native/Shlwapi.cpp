#include "pch.h"
#include "Shlwapi.h"
#include "Shlwapi.g.cpp"
#include <Windows.h>

namespace winrt::Unicord::Universal::Native::implementation {

    bool Shlwapi::InitShlwapiModule = false;
    bool Shlwapi::InitAssocQueryStringW = false;
    bool Shlwapi::InitStrFormatByteSizeEx = false;

    HMODULE Shlwapi::__ShlwapiModule = nullptr;
    StrFormatByteSizeExProc Shlwapi::__StrFormatByteSizeEx = nullptr;
    AssocQueryStringWProc Shlwapi::__AssocQueryStringW = nullptr;

    hstring Shlwapi::AssocQueryString(ASSOCF const& flags, ASSOCSTR const& str, hstring const& pszAssoc, hstring const& pszExtra) {
        InitShlwapi();

        if (!InitAssocQueryStringW) {
            InitAssocQueryStringW = true;

            __AssocQueryStringW = NativeHelpers::GetProcAddress<AssocQueryStringWProc>(__ShlwapiModule, "AssocQueryStringW");
            if (__AssocQueryStringW == nullptr) {
                winrt::check_win32(GetLastError());
            }
        }

        if (__AssocQueryStringW == nullptr)
            throw hresult_not_implemented();

        LPCTSTR raw_pszAssoc = pszAssoc.c_str();
        LPCTSTR raw_pszExtra = pszExtra.empty() ? NULL : pszExtra.c_str();
        ASSOCF raw_flags = flags | ASSOCF::NOTRUNCATE;
        DWORD size = 0;

        winrt::check_hresult(__AssocQueryStringW(raw_flags, str, raw_pszAssoc, raw_pszExtra, NULL, &size));

        std::vector<wchar_t> buff;
        buff.resize(size);

        winrt::check_hresult(__AssocQueryStringW(raw_flags, str, raw_pszAssoc, raw_pszExtra, buff.data(), &size));

        return hstring(buff.data());
    }

    hstring Shlwapi::StrFormatByteSizeEx(uint64_t size, SFBSFlags const& flags) {
        InitShlwapi();

        if (!InitStrFormatByteSizeEx) {
            InitStrFormatByteSizeEx = true;

            __StrFormatByteSizeEx = NativeHelpers::GetProcAddress<StrFormatByteSizeExProc>(__ShlwapiModule, "StrFormatByteSizeEx");
            if (__StrFormatByteSizeEx == nullptr) {
                winrt::check_win32(GetLastError());
            }
        }

        if (__StrFormatByteSizeEx == nullptr)
            throw hresult_not_implemented();

        wchar_t str[32]{ 0 };
        winrt::check_hresult(__StrFormatByteSizeEx(size, flags, str, 32));

        return hstring(str);
    }

    void Shlwapi::InitShlwapi() {
        if (!InitShlwapiModule) {
            InitShlwapiModule = true;

            __ShlwapiModule = NativeHelpers::LoadLibrary(L"shlwapi.dll");
            if (__ShlwapiModule == nullptr) {
                winrt::check_win32(GetLastError());
            }
        }

        if (__ShlwapiModule == nullptr)
            throw hresult_not_implemented();
    }
}

#include "pch.h"
#include "Shlwapi.h"
#include "Shlwapi.g.cpp"

namespace winrt::Unicord::Universal::Native::implementation {
    bool Shlwapi::InitStrFormatByteSizeEx = false;
    HMODULE Shlwapi::__ShlwapiModule = nullptr;
    StrFormatByteSizeExProc Shlwapi::__StrFormatByteSizeEx = nullptr;

    hstring Shlwapi::StrFormatByteSizeEx(uint64_t size, SFBSFlags const& flags) {

        if (!InitStrFormatByteSizeEx) {
            InitStrFormatByteSizeEx = true;

            __ShlwapiModule = NativeHelpers::LoadLibrary(L"shlwapi.dll");
            if (__ShlwapiModule == nullptr) {
                winrt::check_win32(GetLastError());
            }

            __StrFormatByteSizeEx = NativeHelpers::GetProcAddress<StrFormatByteSizeExProc>(__ShlwapiModule, "StrFormatByteSizeEx");
            if (__StrFormatByteSizeEx == nullptr) {
                winrt::check_win32(GetLastError());
            }
        }

        if (__ShlwapiModule == nullptr || __StrFormatByteSizeEx == nullptr)
            throw hresult_not_implemented();

        wchar_t str[32]{ 0 };
        winrt::check_hresult(__StrFormatByteSizeEx(size, flags, str, 32));
        
        return hstring(str);
    }
}

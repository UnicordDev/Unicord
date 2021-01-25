#pragma once
#include "Shlwapi.g.h"
#include "NativeHelpers.h"

namespace winrt::Unicord::Universal::Native::implementation {

    typedef HRESULT(WINAPI* AssocQueryStringWProc)(ASSOCF flags, ASSOCSTR str, LPCWSTR pszAssoc, LPCWSTR pszExtra, LPWSTR pszOut, DWORD* pcchOut);
    typedef HRESULT(WINAPI* StrFormatByteSizeExProc)(ULONGLONG ull, SFBSFlags flags, _Out_writes_(cchBuf) PWSTR pszBuf, _In_range_(>, 0) UINT cchBuf);

    struct Shlwapi : ShlwapiT<Shlwapi> {
        Shlwapi() = default;

        static bool InitShlwapiModule;
        static bool InitAssocQueryStringW;
        static bool InitStrFormatByteSizeEx;

        static HMODULE __ShlwapiModule;
        static AssocQueryStringWProc __AssocQueryStringW;
        static StrFormatByteSizeExProc __StrFormatByteSizeEx;

        static void InitShlwapi();
        static hstring AssocQueryString(ASSOCF const& flags, ASSOCSTR const& str, hstring const& pszAssoc, hstring const& pszExtra);
        static hstring StrFormatByteSizeEx(uint64_t size, SFBSFlags const& flags);
    };
}
namespace winrt::Unicord::Universal::Native::factory_implementation {
    struct Shlwapi : ShlwapiT<Shlwapi, implementation::Shlwapi> {
    };
}

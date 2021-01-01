#pragma once
#include "Shlwapi.g.h"
#include "NativeHelpers.h"

namespace winrt::Unicord::Universal::Native::implementation {

    typedef HRESULT(WINAPI* StrFormatByteSizeExProc)(ULONGLONG ull, SFBSFlags flags, _Out_writes_(cchBuf) PWSTR pszBuf, _In_range_(>, 0) UINT cchBuf);

    struct Shlwapi : ShlwapiT<Shlwapi> {
        Shlwapi() = default;
        static bool InitStrFormatByteSizeEx;
        static HMODULE __ShlwapiModule;
        static StrFormatByteSizeExProc __StrFormatByteSizeEx;

        static hstring StrFormatByteSizeEx(uint64_t size, SFBSFlags const& flags);
    };
}
namespace winrt::Unicord::Universal::Native::factory_implementation {
    struct Shlwapi : ShlwapiT<Shlwapi, implementation::Shlwapi> {
    };
}

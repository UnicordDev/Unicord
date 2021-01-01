#pragma once
#include <Windows.h>
#include <string>

// Unfortunately, there's no good way to represent this class in IDL :/

namespace winrt::Unicord::Universal::Native {

    typedef HMODULE(WINAPI* GetModuleHandleProc)(_In_opt_ LPCTSTR);
    typedef HMODULE(WINAPI* LoadLibraryProc)(_In_ LPCTSTR);
    typedef HMODULE(WINAPI* LoadLibraryExProc)(_In_ LPCTSTR, _Reserved_ HANDLE, _In_ DWORD);

    class NativeHelpers {
    public:
        static HMODULE GetKernelModule();
        static HMODULE LoadLibrary(const std::wstring&);
        static HMODULE LoadLibraryEx(const std::wstring&, HANDLE hFile, DWORD flags);

        template <typename T>
        static const T GetKernelProcAddress(const std::string& procName) {
            return GetProcAddress<T>(GetKernelModule(), procName);
        }

        template <typename T>
        static const T GetProcAddress(HMODULE mod, const std::string& procName) {
            return reinterpret_cast<T>(::GetProcAddress(mod, procName.c_str()));
        }
    };
}
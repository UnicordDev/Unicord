#pragma once
#include "pch.h"
#include "NativeHelpers.h"

namespace winrt::Unicord::Universal::Native {
    HMODULE NativeHelpers::GetKernelModule() {
        static HMODULE kernelModule;

        if (kernelModule == nullptr) {
            MEMORY_BASIC_INFORMATION mbi;
            if (VirtualQuery(VirtualQuery, &mbi, sizeof(MEMORY_BASIC_INFORMATION))) {
                kernelModule = reinterpret_cast<HMODULE>(mbi.AllocationBase);
            }
        }

        return kernelModule;
    }

    HMODULE NativeHelpers::LoadLibrary(const std::wstring& lpLibFileName) {
        static LoadLibraryProc __LoadLibrary;
        if (__LoadLibrary == nullptr) {
            __LoadLibrary = GetKernelProcAddress<LoadLibraryProc>("LoadLibraryW");
        }

        return __LoadLibrary(lpLibFileName.c_str());
    }

    HMODULE NativeHelpers::LoadLibraryEx(const std::wstring& lpLibFileName, HANDLE hFile, DWORD flags) {
        static LoadLibraryExProc __LoadLibraryEx;
        if (__LoadLibraryEx == nullptr) {
            __LoadLibraryEx = GetKernelProcAddress<LoadLibraryExProc>("LoadLibraryExW");
        }

        return __LoadLibraryEx(lpLibFileName.c_str(), hFile, flags);
    }
}
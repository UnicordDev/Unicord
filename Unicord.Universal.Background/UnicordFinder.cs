using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement.Core;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace Unicord.Universal.Background
{
    internal class UnicordFinder
    {
        public static unsafe (HWND hWndUnicord, HWND hWndApplicationFrameHost) FindUnicordWindow()
        {
            uint packageNameLen;
            if (GetCurrentPackageFullName(&packageNameLen, null) == WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE)
                throw new InvalidOperationException();

            string packageName;
            char[] packageNameChars = new char[packageNameLen];
            fixed (char* packageNameCharsPtr = packageNameChars)
            {
                if (GetCurrentPackageFullName(&packageNameLen, packageNameCharsPtr) != WIN32_ERROR.ERROR_SUCCESS)
                    throw new InvalidOperationException();

                packageName = new string(packageNameCharsPtr);
            }

            HWND hWndUnicord = HWND.Null, hWndApplicationFrame = HWND.Null;

            unsafe BOOL IsUnicord(HWND hWnd, LPARAM lParam)
            {
                string className;
                char[] name = new char[MAX_PATH];
                fixed (char* namePtr = name)
                {
                    if (GetClassName(hWnd, namePtr, (int)MAX_PATH) <= 0)
                        return true;

                    className = new string(namePtr);
                }

                if (string.CompareOrdinal(className, "ApplicationFrameWindow") != 0)
                    return true;

                HWND hWndChild = FindWindowEx(hWnd, HWND.Null, "Windows.UI.Core.CoreWindow", null);
                if (hWndChild == null)
                    return true;

                uint procId;
                if (GetWindowThreadProcessId(hWndChild, &procId) == 0)
                    return true;

                HANDLE hProcess = OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, procId);
                if (hProcess == null)
                    return true;

                try
                {
                    uint fullNameLen;
                    if (GetPackageFullName(hProcess, &fullNameLen, null) != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                        return true;

                    string fullName;
                    char[] fullNameChars = new char[fullNameLen];
                    fixed (char* fullNameCharsPtr = fullNameChars)
                    {
                        if (GetPackageFullName(hProcess, &fullNameLen, fullNameCharsPtr) != WIN32_ERROR.ERROR_SUCCESS)
                            return true;

                        fullName = new string(fullNameCharsPtr);
                    }

                    if (packageName == fullName)
                    {
                        hWndUnicord = hWndChild;
                        hWndApplicationFrame = hWnd;
                        return false;
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }

                return false;
            }

            EnumWindows(IsUnicord, IntPtr.Zero);

            return (hWndUnicord, hWndApplicationFrame);
        }

        public static unsafe bool IsUnicordVisible()
        {
            var (hWndUnicord, hWndApplicationFrameHost) = FindUnicordWindow();

            if (hWndUnicord == HWND.Null || hWndApplicationFrameHost == HWND.Null) 
                return false;

            if (!IsWindowVisible(hWndApplicationFrameHost) || !IsWindowVisible(hWndApplicationFrameHost))
                return false;

            if (IsIconic(hWndApplicationFrameHost) || IsIconic(hWndUnicord))
                return false;

            uint dwIsCloaked;

            if (DwmGetWindowAttribute(hWndUnicord, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &dwIsCloaked, sizeof(uint)).Succeeded
                && dwIsCloaked != 0)
                return false;

            if (DwmGetWindowAttribute(hWndApplicationFrameHost, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &dwIsCloaked, sizeof(uint)).Succeeded
                && dwIsCloaked != 0)
                return false;

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;

namespace Unicord.Universal.Background
{
    internal class UxThemePrivate
    {
        public enum PreferredAppMode : int
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        };

        [DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern BOOL AllowDarkModeForWindow(HWND hwnd, BOOL allow);

        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern BOOL SetPreferredAppMode(PreferredAppMode preferredAppMode);

        [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern void FlushMenuThemes();
    }
}

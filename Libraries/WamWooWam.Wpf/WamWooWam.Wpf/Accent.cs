using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WamWooWam.Wpf.Interop;

namespace WamWooWam.Wpf
{
    // more code from https://github.com/riverar/sample-win32-acrylicblur

    public static class Accent
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, (int)dwNewLong));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, long dwNewLong);

        public static void AcrylicWindow(Window wind, byte opacity)
        {
            if (OSVersion.IsWindows10)
            {
                var background = (wind.Background as SolidColorBrush)?.Color ?? Colors.Transparent; ;

                if (!OSVersion.IsWindows10AprilUpdate)
                {
                    wind.Background = new SolidColorBrush(background) { Opacity = opacity / 255f };
                }
                else
                {
                    background.A = (byte)(opacity - 1);
                    wind.Background = new SolidColorBrush(background) { Opacity = 1 / 255f };
                }

                var source = (HwndSource)PresentationSource.FromVisual(wind);
                SetAccentState(source.Handle, AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, background);
            }
        }

        public static void SetAccentState(IntPtr wind, AccentState state, Color? col = null)
        {
            col = col ?? Themes.CurrentConfiguration.GetAccentColour();
            if (state == AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND && !OSVersion.IsWindows10AprilUpdate)
                state = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accent = new AccentPolicy { AccentState = state, GradientColor = (uint)((col.Value.A << 24) | ((col.Value.B << 16 | col.Value.G << 8 | col.Value.R) & 0xFFFFFF)) };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(wind, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public static void SetDWMBlurBehind(IntPtr wind, bool enable)
        {
            var dwmbb = new DWM_BLURBEHIND(enable);
            DwmEnableBlurBehindWindow(wind, ref dwmbb);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WamWooWam.Wpf
{
    public static class OSVersion
    {
        internal static Lazy<Version> _osVersionLazy = new Lazy<Version>(() => Environment.OSVersion.Version);

        public static bool IsWindows10 => _osVersionLazy.Value.Major == 10;
        public static bool IsWindows8 => _osVersionLazy.Value.Major == 6 && (_osVersionLazy.Value.Minor == 2 || _osVersionLazy.Value.Minor == 3);
        public static bool IsWindows7 => _osVersionLazy.Value.Major == 6 && _osVersionLazy.Value.Minor == 1;
        public static bool IsNotWindows7 => !IsWindows7;

        public static bool IsWindows10AprilUpdate => IsWindows10 && _osVersionLazy.Value.Build >= 17134;
    }
}

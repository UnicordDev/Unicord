using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using WamWooWam.Wpf.Interop;

namespace WamWooWam.Wpf.Theme
{
    partial class Menus : ResourceDictionary
    {
        public Menus()
        {
            InitializeComponent();
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (OSVersion.IsWindows10)
            {
                var s = sender as ContextMenu;                
                var res = s.FindResource("SystemChromeLowBrush") as SolidColorBrush;
                var col = res.Color;

                if (!OSVersion.IsWindows10AprilUpdate)
                {
                    var c = res.Clone();
                    c.Opacity = 0.66;
                    s.Background = c;
                }
                else
                {
                    col.A = 127;

                    var c = res.Clone();
                    c.Opacity = 0.01;
                    s.Background = c;
                }

                var source = (HwndSource)PresentationSource.FromVisual(s);
                Accent.SetAccentState(source.Handle, AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, col);
            }
        }

        private void MenuItem_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

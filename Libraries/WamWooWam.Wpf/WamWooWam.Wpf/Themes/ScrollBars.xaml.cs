using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace WamWooWam.Wpf.Theme
{
    partial class ScrollBars : ResourceDictionary
    {
        public ScrollBars()
        {
            InitializeComponent();
        }

        //private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        //{
        //    var scrollViewer = sender as ScrollViewer;
        //    scrollViewer.ScrollToVerticalOffset((scrollViewer.VerticalOffset - (e.Delta * 1.5)).Clamp(0, scrollViewer.ScrollableHeight));
        //    e.Handled = true;
        //}
    }
}
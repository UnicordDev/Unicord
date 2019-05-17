using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ErrorDialog : ContentDialog
    {
        public string Icon { get => iconText.Text; set => iconText.Text = value; }
        public new string Title { get => errorTitle.Text; set => errorTitle.Text = value; }
        public new string Content { get => errorContent.Text; set => errorContent.Text = value; }

        public ErrorDialog()
        {
            InitializeComponent();
        }
    }
}

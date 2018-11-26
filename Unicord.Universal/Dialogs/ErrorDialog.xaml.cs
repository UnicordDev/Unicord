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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ErrorDialog : ContentDialog
    {
        public string Text { get => errorText.Text; set => errorText.Text = value; }
        public string AdditionalText { get => errorInfoText.Text; set => errorInfoText.Text = value; }

        public ErrorDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            if(errorInfoPanel.Visibility == Visibility.Visible)
            {
                SecondaryButtonText = "Show details";
                errorInfoPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SecondaryButtonText = "Hide details";
                errorInfoPanel.Visibility = Visibility.Visible;
            }
        }

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if(!string.IsNullOrWhiteSpace(AdditionalText))
            {
                SecondaryButtonText = "Show details";
            }
        }
    }
}

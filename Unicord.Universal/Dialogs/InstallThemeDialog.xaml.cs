using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    public sealed partial class InstallThemeDialog : ContentDialog
    {
        public InstallThemeDialog(Theme theme)
        {
            InitializeComponent();
            DataContext = theme;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}

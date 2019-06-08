using Windows.UI.Xaml.Controls;

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

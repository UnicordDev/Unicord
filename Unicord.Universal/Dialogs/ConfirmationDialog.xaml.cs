using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ConfirmationDialog : ContentDialog
    {
        public string Icon { get => iconText.Text; set => iconText.Text = value; }
        public new string Title { get => questionTitle.Text; set => questionTitle.Text = value; }
        public new string Content { get => questionContent.Text; set => questionContent.Text = value; }

        public ConfirmationDialog()
        {
            InitializeComponent();
        }
    }
}

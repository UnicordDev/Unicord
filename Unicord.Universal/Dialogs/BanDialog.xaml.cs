using DSharpPlus.Entities;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class BanDialog : ContentDialog
    {
        public int DeleteMessageDays
        {
            get
            {
                switch (deleteMessagesBox.SelectedIndex)
                {
                    case 1:
                        return 1;
                    case 2:
                        return 7;
                    default:
                        return 0;
                }
            }
        }

        public string BanReason
            => banReasonText.Text;

        public BanDialog(DiscordMember member)
        {
            InitializeComponent();
            var resources = ResourceLoader.GetForCurrentView("Dialogs");
            headerTextBlock.Text = string.Format(resources.GetString("BanDialogHeaderFormat"), member.Username, member.Discriminator);
        }
    }
}

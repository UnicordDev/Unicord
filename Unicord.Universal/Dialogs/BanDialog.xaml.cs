using DSharpPlus.Entities;
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
            headerTextBlock.Text = $"Ban @{member.Username}#{member.Discriminator}?";
        }
    }
}

using DSharpPlus.Entities;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class KickDialog : ContentDialog
    {
        public string KickReason
            =>  kickReasonText.Text;

        public KickDialog(DiscordMember member)
        {
            InitializeComponent();
            headerTextBlock.Text = $"Kick @{member.Username}#{member.Discriminator}?";
        }
    }
}

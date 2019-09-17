using DSharpPlus.Entities;
using Windows.ApplicationModel.Resources;
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
            var resources = ResourceLoader.GetForCurrentView("Dialogs");
            headerTextBlock.Text = string.Format(resources.GetString("KickDialogHeaderFormat"), member.Username, member.Discriminator);
        }
    }
}

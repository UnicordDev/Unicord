using DSharpPlus.Entities;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ChangeNicknameDialog : ContentDialog
    {
        public string Text { get => inputBox.Text; set => inputBox.Text = value; }

        public ChangeNicknameDialog(DiscordMember user)
        {
            InitializeComponent();
            inputBox.Text = user.Nickname ?? "";
            inputBox.PlaceholderText = user.Username;
        }
    }
}

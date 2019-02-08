using DSharpPlus.Entities;
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

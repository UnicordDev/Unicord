using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class PinMessageDialog : ContentDialog
    {
        public PinMessageDialog(DiscordMessage message)
        {
            InitializeComponent();
            Title = message.Pinned ? "Unpin this message?" : "Pin this message?";
            Content = new MessageViewer() { Message = message, IsEnabled = false, Background = new SolidColorBrush(Colors.Transparent) };
        }
    }
}

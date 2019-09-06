using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Controls
{
    public sealed class SystemMessageViewer : Control
    {
        public DiscordMessage Message
        {
            get { return (DiscordMessage)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(DiscordMessage), typeof(SystemMessageViewer), new PropertyMetadata(null));

        public SystemMessageViewer()
        {
            this.DefaultStyleKey = typeof(SystemMessageViewer);
        }
    }
}

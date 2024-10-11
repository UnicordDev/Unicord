using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Messages
{
    public sealed class MessageEditTools : Control
    {
        public MessageEditViewModel ViewModel
        {
            get { return (MessageEditViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(MessageEditViewModel), typeof(MessageEditTools), new PropertyMetadata(null));

        public MessageEditTools()
        {
            this.DefaultStyleKey = typeof(MessageEditTools);
        }
    }
}

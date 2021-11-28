using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Controls.Messages
{
    public sealed class AttachmentControl : Control
    {
        public DiscordAttachment Attachment
        {
            get => (DiscordAttachment)GetValue(AttachmentProperty);
            set => SetValue(AttachmentProperty, value);
        }

        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register("Attachment", typeof(DiscordAttachment), typeof(AttachmentControl), new PropertyMetadata(null, OnAttachmentPropertyChanged));

        public AttachmentViewModel ViewModel
        {
            get { return (AttachmentViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(AttachmentViewModel), typeof(AttachmentControl), new PropertyMetadata(null));

        private static void OnAttachmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AttachmentControl)d;
            control.ViewModel = new AttachmentViewModel(control.Attachment);
        }

        public AttachmentControl()
        {
            this.DefaultStyleKey = typeof(AttachmentControl);
            this.Tapped += OnTapped;
        }

        private async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (Attachment != null)
                await OverlayService.GetForCurrentView()
                                    .ShowOverlayAsync<AttachmentOverlayPage>(Attachment);
        }
    }
}

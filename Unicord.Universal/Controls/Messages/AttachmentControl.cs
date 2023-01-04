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
        public AttachmentViewModel ViewModel
        {
            get { return (AttachmentViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(AttachmentViewModel), typeof(AttachmentControl), new PropertyMetadata(null));

        public AttachmentControl()
        {
            this.DefaultStyleKey = typeof(AttachmentControl);
            this.Tapped += OnTapped;
        }

        private async void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel == null || ViewModel.Type != AttachmentType.Image)
                return;

            await OverlayService.GetForCurrentView()
                                .ShowOverlayAsync<AttachmentOverlayPage>(ViewModel);
        }
    }
}

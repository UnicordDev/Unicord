using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Messages
{
    public sealed class AttachmentMediaControl : Control, INotifyPropertyChanged
    {
        private MediaPlayerElement _mediaPlayerElement;
        private Border _mediaPlayerBorder;

        public AttachmentViewModel ViewModel
        {
            get { return (AttachmentViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(AttachmentViewModel), typeof(AttachmentMediaControl), new PropertyMetadata(null, OnAttachmentChange));

        public event PropertyChangedEventHandler PropertyChanged;

        private static void OnAttachmentChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AttachmentMediaControl)d).OnAttachmentChanged(e);
        }

        public AttachmentMediaControl()
        {
            this.DefaultStyleKey = typeof(AttachmentMediaControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("MediaPlayer") is not MediaPlayerElement mediaPlayerElement)
                return;

            _mediaPlayerElement = mediaPlayerElement;

            if (GetTemplateChild("MediaPlayerBorder") is not Border mediaPlayerBorder)
                return;

            _mediaPlayerBorder = mediaPlayerBorder;

            if (mediaPlayerElement.TransportControls is not CustomMediaTransportControls transportControls)
                return;

            //mediaPlayerBorder.DataContext = this.ViewModel;
            transportControls.FullWindowRequested += OnFullWindowRequested;
        }

        private void OnAttachmentChanged(DependencyPropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));

            //if (e.NewValue != null && e.NewValue is AttachmentViewModel vm)
            //{
            //    if (!ApplyTemplate()) return;

            //    if (GetTemplateChild("MediaPlayer") is not MediaPlayerElement mediaPlayerElement)
            //        return;

            //    if (vm.Type == AttachmentType.Audio)
            //    {
            //        mediaPlayerElement.TransportControls.Style = (Style)Application.Current.Resources["AudioMediaTransportControlsStyle"];
            //        mediaPlayerElement.TransportControls.IsCompact = true;
            //    }
            //    //else if (vm.Type == AttachmentType.Video)
            //    //{
            //    //    mediaPlayerElement.TransportControls.Style = (Style)Application.Current.Resources[typeof(CustomMediaTransportControls)];
            //    //}
            //}
        }

        private void OnFullWindowRequested(object sender, EventArgs e)
        {
            _mediaPlayerElement.IsFullWindow = !_mediaPlayerElement.IsFullWindow;
        }
    }
}

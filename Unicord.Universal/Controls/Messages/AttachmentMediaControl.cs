using System;
using System.ComponentModel;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Messages
{
    [Obsolete]
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
        }

        private void OnAttachmentChanged(DependencyPropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));

            if (e.NewValue != null && e.NewValue is AttachmentViewModel vm)
            {
                if (!ApplyTemplate()) return;

                //if (vm.Type == AttachmentType.Audio)
                //{
                //    GoToElementStateCore("Audio", false);
                //}
                //else if(vm.Type == AttachmentType.Video)
                //{
                //    GoToElementStateCore("Video", false);
                //}
            }
        }
    }
}

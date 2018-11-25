using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls
{
    public sealed partial class MediaPlayerControl : UserControl
    {
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(MediaPlayerElement), new PropertyMetadata(null));

        public Uri PosterSource
        {
            get { return (Uri)GetValue(PosterSourceProperty); }
            set { SetValue(PosterSourceProperty, value); }
        }

        public static readonly DependencyProperty PosterSourceProperty =
            DependencyProperty.Register("PosterSource", typeof(Uri), typeof(MediaPlayerElement), new PropertyMetadata(null, PosterChanged));

        public bool AudioOnly
        {
            get { return (bool)GetValue(AudioOnlyProperty); }
            set { SetValue(AudioOnlyProperty, value); }
        }

        public static readonly DependencyProperty AudioOnlyProperty =
            DependencyProperty.Register("AudioOnly", typeof(bool), typeof(MediaPlayerElement), new PropertyMetadata(false));

        private const string PLAY_CHAR = "\xE768";
        private const string PAUSE_CHAR = "\xE769";

        private int _framesSinceLastMove = 0;
        private bool _isPlaying = false;
        private DiscordAttachment _attachment;
        private DispatcherTimer _timer;
        private bool _sourceSet;
        private bool _isFullscreen;
        private bool _scrubbing;
        private bool _wasPlaying;

        public MediaPlayerControl()
        {
            InitializeComponent();

            _timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000 / 15) };
            _timer.Tick += _timer_Tick;
        }

        public MediaPlayerControl(DiscordAttachment attach, bool audioOnly = false) : this()
        {
            _attachment = attach;
            AudioOnly = audioOnly;

            Source = new Uri(attach.Url);
            Margin = new Thickness(0, 5, 0, 0);

            if (!audioOnly)
            {
                PosterSource = new Uri(attach.ProxyUrl + "?format=jpeg");
                HorizontalAlignment = HorizontalAlignment.Left;
                fullscreenButton.Visibility = Visibility.Visible;
                downloadButton.Visibility = Visibility.Collapsed;
                transportControls.Visibility = Visibility.Collapsed;
                //attachmentDetails.ImageUri = new Uri(attach.Url);
                //attachmentDetails.Visibility = Visibility.Visible;
            }
            else
            {
                Height = 48;
                MaxWidth = 640;
                topLevelContainer.Visibility = Visibility.Collapsed;
                fullscreenButton.Visibility = Visibility.Collapsed;
                downloadButton.Visibility = Visibility.Visible;
                //attachmentDetails.Visibility = Visibility.Collapsed;
            }
        }

        public void Play()
        {
            if (!_sourceSet)
            {
                SetupMediaElement();
            }
            else
            {
                if (mediaElement.NaturalDuration.HasTimeSpan && mediaElement.Position == mediaElement.NaturalDuration.TimeSpan)
                {
                    mediaElement.Position = TimeSpan.Zero;
                }
            }

            playPauseButton.Content = PAUSE_CHAR;
            mediaElement.Play();
            _isPlaying = true;
            if (!_timer.IsEnabled)
                _timer.Start();
        }


        public void Pause()
        {
            playPauseButton.Content = PLAY_CHAR;
            mediaElement.Pause();
            _isPlaying = false;
            _timer.Stop();
            if (!mediaElement.AreTransportControlsEnabled)
                transportControls.Visibility = Visibility.Visible;
            _framesSinceLastMove = 0;

            //if (!AudioOnly)
            //{
            //    attachmentDetails.Visibility = Visibility.Visible;
            //}
        }

        private void SetupMediaElement()
        {
            mediaElement.Source = Source;

            if (AudioOnly)
            {
                mediaElement.Visibility = Visibility.Collapsed;
                //attachmentDetails.Visibility = Visibility.Collapsed;
            }
            else
            {
                //attachmentDetails.Visibility = Visibility.Visible;
            }

            transportControls.Visibility = Visibility.Visible;
            posterContainer.Visibility = Visibility.Collapsed;
            posterImage.Visibility = Visibility.Collapsed;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (AudioOnly)
            {
                return new Size(Math.Min(640, constraint.Width), 48);
            }
            else if (!_isFullscreen)
            {
                var width = _attachment.Width;
                var height = _attachment.Height;

                WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, 640, 480);
                WamWooWam.Core.Drawing.ScaleProportions(ref width, ref height, double.IsInfinity(constraint.Width) ? 640 : (int)constraint.Width, double.IsInfinity(constraint.Height) ? 480 : (int)constraint.Height);

                return new Size(width, height);
            }
            else
            {
                return base.MeasureOverride(constraint);
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!_sourceSet)
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    volumeSlider.Value = mediaElement.Volume;
                    position.Minimum = 0;
                    position.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
                    total.Text = mediaElement.NaturalDuration.TimeSpan.ToString("mm\\:ss");
                }
            }
            else
            {
                mediaElement.Position = TimeSpan.FromMilliseconds(position.Value);
            }

            _sourceSet = true;

            if (!_isPlaying)
                Play();
        }

        private void _timer_Tick(object sender, object e)
        {
            if (!AudioOnly && _framesSinceLastMove == 90)
            {
                transportControls.Visibility = Visibility.Collapsed;
                //attachmentDetails.Visibility = Visibility.Collapsed;
            }

            _framesSinceLastMove += 1;

            if (_sourceSet)
            {
                if (mediaElement.NaturalDuration.HasTimeSpan && mediaElement.Position == mediaElement.NaturalDuration.TimeSpan)
                {
                    Pause();
                }

                position.Value = mediaElement.Position.TotalMilliseconds;
                elapsed.Text = mediaElement.Position.ToString("mm\\:ss");
            }
        }

        private static void PosterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MediaPlayerControl).mediaElement.PosterSource = new BitmapImage(e.NewValue as Uri);
        }

        private void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Play();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void FullscreenButton_Checked(object sender, RoutedEventArgs e)
        {
            _isFullscreen = true;
            mediaElement.IsFullWindow = true;
            mediaElement.AreTransportControlsEnabled = true;
            transportControls.Visibility = Visibility.Collapsed;
        }

        private void FullscreenButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _isFullscreen = false;
        }

        private void UserControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _framesSinceLastMove = 0;
            if (_sourceSet && !mediaElement.AreTransportControlsEnabled)
                transportControls.Visibility = Visibility.Visible;
            //attachmentDetails.Visibility = Visibility.Visible;
        }

        private void Position_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            mediaElement.Position = TimeSpan.FromMilliseconds(e.NewValue);
        }

        private void VolumeButton_Checked(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as ToggleButton);
        }

        private void Flyout_Closed(object sender, object e)
        {
            volumeButton.IsChecked = false;
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void IsMutedButton_Checked(object sender, RoutedEventArgs e)
        {
            mediaElement.IsMuted = true;
            (sender as ToggleButton).Content = "\xE74F";
        }

        private void IsMutedButton_Unchecked(object sender, RoutedEventArgs e)
        {
            mediaElement.IsMuted = false;
            (sender as ToggleButton).Content = "\xE767";
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            mediaElement.Volume = e.NewValue;
        }

        private void Position_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _wasPlaying = _isPlaying;

            if (_isPlaying)
                Pause();

            _scrubbing = true;
        }

        private void Position_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _scrubbing = false;
            if (_wasPlaying)
                Play();
        }
    }
}

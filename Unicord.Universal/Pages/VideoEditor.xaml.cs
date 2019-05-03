using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoEditor : Page
    {
        public const string PLAY_GLYPH = "\xE768";
        public const string PAUSE_GLYPH = "\xE769";

        private DispatcherTimer _playTimer;
        private EditedFileUploadModel _model;
        private MediaStreamSource _mediaStreamSource;
        private bool _ready;
        private bool _succeeded;
        private StorageFile _tempFile;
        private IAsyncOperationWithProgress<TranscodeFailureReason, double> _renderTask;
        private bool _playing;
        private bool _wasPlaying;
        private bool _playbackSkip;
        private bool _scrubSkip;
        private DispatcherTimer _resizeTimer;
        private double _startPosition;

        public VideoEditor()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is EditedFileUploadModel edited)
            {
                _model = edited;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                await StatusBar.GetForCurrentView().HideAsync();
            }
            else
            {
                topGrid.Padding = App.StatusBarFill;
            }

            if (_model.StorageFile == null)
            {
                await UIUtilities.ShowErrorDialogAsync("This clip cannot be edited.", "Currently, you can only edit video clips from files. Sorry!");
                Close();
                return;
            }

            if (_model.Composition == null)
            {
                var name = Path.ChangeExtension(_model.StorageFile.Name, "uni-cmp");
                try
                {
                    _model.CompositionFile = _model.CompositionFile ?? await ApplicationData.Current.LocalFolder.GetFileAsync(name);
                    if (await UIUtilities.ShowYesNoDialogAsync("Load previous edit?", "It looks like you've edited this file before, do you want to load your previous edits?"))
                    {
                        _model.Composition = await MediaComposition.LoadAsync(_model.CompositionFile);
                    }
                    else
                    {
                        _model.Composition = new MediaComposition();
                    }
                }
                catch
                {
                    _model.Composition = new MediaComposition();
                }
            }

            if (_model.Composition.Clips.Count == 0)
            {
                var clip = await MediaClip.CreateFromFileAsync(_model.StorageFile);

                _model.Composition.Clips.Add(clip);
                _model.Clip = clip;
            }
            else
            {
                _model.Clip = _model.Composition.Clips.First();
            }

            rangeSelector.Maximum = _model.Clip.OriginalDuration.TotalSeconds;
            rangeSelector.RangeMin = _model.Clip.TrimTimeFromStart.TotalSeconds;
            rangeSelector.RangeMax = _model.Clip.OriginalDuration.TotalSeconds - _model.Clip.TrimTimeFromEnd.TotalSeconds;
            rangeSelector.Value = rangeSelector.RangeMin;
            startPointText.Text = _model.Clip.TrimTimeFromStart.ToString("mm\\:ss");
            endPointText.Text = (_model.Clip.OriginalDuration - _model.Clip.TrimTimeFromEnd).ToString("mm\\:ss");

            _ready = true;
            UpdateMediaElementSource();
        }

        public void UpdateMediaElementSource()
        {
            if (!_ready || _model == null)
            {
                return;
            }

            if (mediaElement.MediaPlayer?.PlaybackSession != null)
            {
                mediaElement.MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            }

            _mediaStreamSource = _model.Composition.GeneratePreviewMediaStreamSource(Math.Min((int)mediaElement.ActualWidth, 1280), Math.Min((int)mediaElement.ActualHeight, 720));

            if (_mediaStreamSource != null)
            {
                mediaElement.Source = MediaSource.CreateFromMediaStreamSource(_mediaStreamSource);
                mediaElement.MediaPlayer.Volume = VolumeSlider.Value;
                mediaElement.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Math.Min(Math.Max(0, rangeSelector.Value - rangeSelector.RangeMin), rangeSelector.RangeMax));
                mediaElement.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            }
        }


        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (await UIUtilities.ShowYesNoDialogAsync("Save your changes?", "Do you want to save your changes? They'll be loaded the next time you edit this clip."))
            {
                await SaveComposition();
            }

            Close();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizePreviewTimer();
        }

        private void RangeSelector_RangeChanged(object sender, Controls.RangeChangedEventArgs e)
        {
            if (!_ready)
            {
                return;
            }

            if (_playing)
            {
                Pause();
                _wasPlaying = true;
            }

            _scrubSkip = true;
            _playbackSkip = true;

            if (_startPosition == 0d)
            {
                _startPosition = _model.Clip.TrimTimeFromStart.TotalSeconds;
            }

            var value = 0d;
            switch (e.ChangedRangeProperty)
            {
                case Controls.RangeSelectorProperty.MinimumValue:
                    _model.Clip.TrimTimeFromStart = TimeSpan.FromSeconds(Math.Max(0, e.NewValue));
                    rangeSelector.Value = rangeSelector.RangeMin;
                    value = Math.Min(Math.Max(0, _startPosition - rangeSelector.RangeMin), rangeSelector.RangeMax);
                    mediaElement.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(value);
                    break;
                case Controls.RangeSelectorProperty.MaximumValue:
                    _model.Clip.TrimTimeFromEnd = TimeSpan.FromSeconds(Math.Max(0, _model.Clip.OriginalDuration.TotalSeconds - e.NewValue));
                    rangeSelector.Value = rangeSelector.RangeMax;
                    value = Math.Max(Math.Min(rangeSelector.RangeMax, _model.Clip.TrimmedDuration.TotalSeconds), rangeSelector.RangeMin);
                    mediaElement.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(value - 0.1);
                    break;
                default:
                    break;
            }

            startPointText.Text = TimeSpan.FromSeconds(Math.Max(0, rangeSelector.RangeMin)).ToString("mm\\:ss");
            endPointText.Text = TimeSpan.FromSeconds(Math.Min(_model.Clip.OriginalDuration.TotalSeconds, rangeSelector.RangeMax)).ToString("mm\\:ss");

            ResizePreviewTimer();
        }

        private void RangeSelector_ValueChanged(object sender, double e)
        {
            if (!_ready)
            {
                return;
            }

            if (_playing)
            {
                if (_playbackSkip)
                {
                    _playbackSkip = false;
                    return;
                }

                Pause();
                _wasPlaying = true;
            }

            _scrubSkip = true;

            mediaElement.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Math.Min(Math.Max(0, e - rangeSelector.RangeMin), mediaElement.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds));
            RestartPlayTimer();
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            progressText.Text = "0";
            progressText.Visibility = Visibility.Visible;
            completedText.Visibility = Visibility.Collapsed;

            overlayGrid.Visibility = Visibility.Visible;
            OpenProcessingOverlay.Begin();

            var props = await (_model.StorageFile as StorageFile).Properties.GetVideoPropertiesAsync();
            var profile = MediaTranscoding.CreateVideoEncodingProfileFromProps(true, props);

            if (_tempFile == null)
            {
                _tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(_model.StorageFile.Name, CreationCollisionOption.GenerateUniqueName);
            }

            await SaveComposition();

            _renderTask = _model.Composition.RenderToFileAsync(_tempFile, MediaTrimmingPreference.Fast, profile);
            _renderTask.Progress = OnProgress;
            _renderTask.Completed = OnCompleted;
        }

        private async Task SaveComposition()
        {
            if (_model.CompositionFile == null)
            {
                _model.CompositionFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Path.ChangeExtension(_model.StorageFile.Name, "uni-cmp"));
            }

            await _model.Composition.SaveAsync(_model.CompositionFile);
        }

        public async void OnProgress(IAsyncOperationWithProgress<TranscodeFailureReason, double> info, double progress)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                progressRing.Value = progress;
                progressText.Text = $"{progress:F0}";
            });
        }

        private async void OnCompleted(IAsyncOperationWithProgress<TranscodeFailureReason, double> info, AsyncStatus status)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    var results = info.GetResults();

                    if (status == AsyncStatus.Canceled)
                    {
                        return;
                    }

                    if (results != TranscodeFailureReason.None || status != AsyncStatus.Completed)
                    {
                        progressText.Visibility = Visibility.Collapsed;
                        completedText.Text = "\xE711";
                        completedText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        progressText.Visibility = Visibility.Collapsed;
                        completedText.Text = "\xE8FB";
                        completedText.Visibility = Visibility.Visible;
                        _succeeded = true;
                    }
                }
                catch
                {
                    progressText.Visibility = Visibility.Collapsed;
                    completedText.Text = "\xE711";
                    completedText.Visibility = Visibility.Visible;
                }
                finally
                {
                    CloseProcessingOverlay.Begin();
                }
            });

            _renderTask = null;
        }

        private async void CloseProcessingOverlay_Completed(object sender, object e)
        {
            overlayGrid.Visibility = Visibility.Collapsed;

            if (_succeeded && _tempFile != null)
            {
                await _model.UpdateFromStorageFileAsync(_tempFile, isTemporary: true);

                var channelModel = (_model.Parent.DataContext as ChannelViewModel);
                channelModel.FileUploads.Remove(_model);
                channelModel.FileUploads.Add(_model);

                Close();
            }
        }

        private async void Close()
        {
            this.FindParent<DiscordPage>().CloseCustomPane();
            mediaElement.Source = null;
            _mediaStreamSource = null;
            _playTimer?.Stop();
            _model = null;
            _renderTask = null;
            _tempFile = null;

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                await StatusBar.GetForCurrentView().ShowAsync();
            }

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;

            GC.Collect();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _renderTask?.Cancel();
        }

        private void RestartPlayTimer()
        {
            if (_playTimer == null)
            {
                _playTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(50) };
                _playTimer.Tick += _playTimer_Tick;
            }

            _playTimer.Start();
        }

        private void ResizePreviewTimer()
        {
            if (_resizeTimer == null)
            {
                _resizeTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };
                _resizeTimer.Tick += _resizeTimer_Tick;
            }

            _playTimer?.Stop();
            _resizeTimer.Start();
        }

        private void _resizeTimer_Tick(object sender, object e)
        {
            if (_playing)
            {
                Pause();
                _wasPlaying = true;
            }

            UpdateMediaElementSource();
            RestartPlayTimer();

            _resizeTimer.Stop();
        }

        private void _playTimer_Tick(object sender, object e)
        {
            if (!_ready || mediaElement.MediaPlayer == null || mediaElement.MediaPlayer.PlaybackSession == null)
            {
                return;
            }

            _scrubSkip = true;
            mediaElement.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(Math.Min(Math.Max(0, (_startPosition != 0 ? _startPosition : rangeSelector.Value) - rangeSelector.RangeMin), rangeSelector.RangeMax));

            _startPosition = 0;

            if (_wasPlaying)
            {
                _wasPlaying = false;
                Play();
            }

            _playTimer.Stop();
        }

        private void Play()
        {
            _playing = true;
            playPauseButton.Content = PAUSE_GLYPH;
            mediaElement.MediaPlayer.Play();
        }

        private void Pause()
        {
            _playing = false;
            playPauseButton.Content = PLAY_GLYPH;
            mediaElement.MediaPlayer.Pause();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_playing)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (_scrubSkip)
            {
                _scrubSkip = false;
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _playbackSkip = true;
                rangeSelector.Value = Math.Min(rangeSelector.RangeMin + sender.Position.TotalSeconds, rangeSelector.RangeMax);
            });
        }

        private void AudioMuteButton_Click(object sender, RoutedEventArgs e)
        {
            if ((mediaElement.MediaPlayer.IsMuted = !mediaElement.MediaPlayer.IsMuted))
            {
                AudioMuteSymbol.Symbol = Symbol.Mute;
                VolumeSlider.IsEnabled = false;
                VolumeSlider.Value = 0;
            }
            else
            {
                AudioMuteSymbol.Symbol = Symbol.Volume;
                VolumeSlider.IsEnabled = true;
                VolumeSlider.Value = mediaElement.MediaPlayer.Volume * 100;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (mediaElement.MediaPlayer != null && !mediaElement.MediaPlayer.IsMuted)
            {
                mediaElement.MediaPlayer.Volume = e.NewValue / 100d;
            }
        }
    }
}

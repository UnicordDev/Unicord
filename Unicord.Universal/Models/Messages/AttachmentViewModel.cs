using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Commands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Models.Messages
{
    public enum AttachmentType
    {
        Image,
        Audio,
        Video,
        Svg,
        Generic
    }

    public class AttachmentViewModel : ViewModelBase
    {
        // a set of known media file extensions for use as a heuristic
        private static readonly HashSet<string> _mediaExtensions =
              new HashSet<string> { ".gifv", ".mp4", ".mov", ".webm", ".wmv", ".avi", ".mkv", ".ogv", ".mp3", ".m4a", ".aac", ".wav", ".wma", ".flac", ".ogg", ".oga", ".opus", ".mpg", ".mpeg" };

        private SynchronizationContext _syncContext;
        private DiscordAttachment _attachment;
        private AttachmentType _type;
        private Size? _naturalSize;
        private object _source;
        private bool _sourceInvalidated;

        private MediaSource _mediaSource;
        private MediaPlaybackItem _mediaPlaybackItem;

        public AttachmentViewModel(DiscordAttachment attachment)
        {
            _syncContext = SynchronizationContext.Current;
            _attachment = attachment;
            _type = GetAttachmentType(attachment);
            _sourceInvalidated = false;

            DownloadProgress = new ProgressInfo();
            DownloadCommand = new DownloadCommand(attachment.Url, DownloadProgress);

            ShareProgress = new ProgressInfo();
            ShareCommand = new ShareCommand(attachment.Url, attachment.FileName, ShareProgress);
        }

        public string FileName =>
            _attachment.FileName;
        public string FileSize =>
            Tools.ToFileSizeString(_attachment.FileSize);
        public string Url =>
            _attachment.Url;
        public bool IsSpoiler =>
            _attachment.FileName.StartsWith("SPOILER_");
        public double NaturalWidth =>
            _attachment.Width != 0 ? _attachment.Width : _naturalSize?.Width ?? double.NaN;
        public double NaturalHeight =>
            _attachment.Height != 0 ? _attachment.Height : _naturalSize?.Height ?? double.NaN;
        public AttachmentType Type { get => _type; private set => OnPropertySet(ref _type, value); }
        public object Source =>
            GetOrCreateSource();
        public string PosterSource =>
             _attachment.Width != 0 ? _attachment.ProxyUrl + "?format=jpeg" : null;

        public bool IsVideo =>
            Type == AttachmentType.Video;
        public bool IsAudio =>
            Type == AttachmentType.Audio;

        public ICommand DownloadCommand { get; }
        public ProgressInfo DownloadProgress { get; set; }

        public ICommand ShareCommand { get; }
        public ProgressInfo ShareProgress { get; set; }

        public ICommand CopyCommand { get; set; }

        private object GetOrCreateSource()
        {
            if (_source != null && !_sourceInvalidated) return _source;

            _source = null;
            _sourceInvalidated = false;

            if (_mediaSource != null)
            {
                _mediaSource.Dispose();
                _mediaSource = null;
                _mediaPlaybackItem = null;
            }

            switch (_type)
            {
                case AttachmentType.Image:
                    _source = _attachment.ProxyUrl;
                    break;
                case AttachmentType.Svg:
                    _source = new SvgImageSource(new Uri(_attachment.Url)) { RasterizePixelWidth = 640 };
                    break;
                case AttachmentType.Audio:
                case AttachmentType.Video:
                    _mediaSource = MediaSource.CreateFromUri(new Uri(_attachment.Url));
                    _mediaPlaybackItem = new MediaPlaybackItem(_mediaSource);
                    _mediaPlaybackItem.VideoTracksChanged += OnVideoTracksLoaded;
                    _mediaPlaybackItem.AudioTracksChanged += OnAudioTracksLoaded;
                    _source = _mediaPlaybackItem;
                    break;
                case AttachmentType.Generic:
                default:
                    throw new InvalidOperationException("Unable to create source for Generic or Invalid attachment type");
            }

            return _source;
        }

        // these callbacks aren't on the UI thread doofus.
        private void OnVideoTracksLoaded(MediaPlaybackItem sender, IVectorChangedEventArgs args)
        {
            var track = sender.VideoTracks.OrderByDescending(d => { var props = d.GetEncodingProperties(); return props.Width * props.Height; })
                                            .FirstOrDefault();

            if (track != null && _type != AttachmentType.Video)
            {
                var props = track.GetEncodingProperties();

                _naturalSize = new Size(props.Width, props.Height);
                _type = AttachmentType.Video;
                _syncContext.Post((_) =>
                {
                    InvokePropertyChanged(nameof(NaturalWidth));
                    InvokePropertyChanged(nameof(NaturalHeight));
                    InvokePropertyChanged(nameof(Type));
                    InvokePropertyChanged(nameof(IsVideo));
                    InvokePropertyChanged(nameof(IsAudio));
                }, null);
            }
        }

        private void OnAudioTracksLoaded(MediaPlaybackItem sender, IVectorChangedEventArgs args)
        {

        }

        public static AttachmentType GetAttachmentType(DiscordAttachment attachment)
        {
            // we have a bunch of heuristics available to work out the type of attachment we're dealing with
            // this code only uses a couple

            var fileName = attachment.FileName.ToLowerInvariant();
            var fileExtension = Path.GetExtension(fileName);
            var mimeType = attachment.ContentType?.ToLowerInvariant(); // this isn't always available on old messages

            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                // these are easy lmao
                if (mimeType.StartsWith("video")) return AttachmentType.Video;
                if (mimeType.StartsWith("audio")) return AttachmentType.Audio;
                if (mimeType.StartsWith("image")) return AttachmentType.Image;
            }

            // we special case SVG because their sizing logic is whack
            if (fileExtension == ".svg") return AttachmentType.Svg;

            if (_mediaExtensions.Contains(fileExtension))
            {
                // discord doesn't return proper width/height information for some video formats
                // as such, once the media is loaded this may change.
                if (attachment.Width != 0 && attachment.Height != 0)
                    return AttachmentType.Video;

                return AttachmentType.Audio;
            }

            if (attachment.Width != 0 && attachment.Height != 0)
                return AttachmentType.Image;

            return AttachmentType.Generic;
        }
    }
}

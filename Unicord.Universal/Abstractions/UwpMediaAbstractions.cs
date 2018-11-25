using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Abstractions;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.System.Profile;

namespace Unicord.Abstractions
{
    internal class UwpMediaAbstractions : IMediaAbstractions
    {
        private MediaTranscoder _transcoder;

        public UwpMediaAbstractions()
        {
            try
            {
                _transcoder = new MediaTranscoder() { VideoProcessingAlgorithm = MediaVideoProcessingAlgorithm.MrfCrf444, HardwareAccelerationEnabled = true };

            }
            catch { }
        }

        public bool IsTranscodingAvailable => _transcoder != null;

        public async Task<string> GetFileMimeAsync(string path)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            return file.ContentType;
        }

        public async Task<bool> TryTranscodeVideoAsync(IStorageFile storageFile, Stream stream, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            if (!IsTranscodingAvailable)
                return false;

            var props = await (storageFile as IStorageItemProperties).Properties.GetVideoPropertiesAsync();

            var width = (int)props.Width;
            var height = (int)props.Height;
            var bitrate = hq ? (uint)2_000_000 : 1_115_000;

#if WINDOWS_WPF
            var maxWidth = Settings.GetSetting("VideoWidth", 640);
            var maxHeight = Settings.GetSetting("VideoHeight", 360);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            bitrate = Settings.GetSetting("VideoBitrate", bitrate); // theoretically 60s of video / 8MB 

#elif WINDOWS_UWP
            var maxWidth = Universal.App.RoamingSettings.Read("VideoWidth", 854);
            var maxHeight = Universal.App.RoamingSettings.Read("VideoHeight", 480);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            bitrate = Universal.App.RoamingSettings.Read("VideoBitrate", bitrate);
#endif

            var container = new ContainerEncodingProperties()
            {
                Subtype = MediaEncodingSubtypes.Mpeg4
            };

            var profile = new MediaEncodingProfile() { Container = container };
            var video = new VideoStreamDescriptor(new VideoEncodingProperties()
            {
                Width = (uint)(Math.Round(width / 2.0) * 2),
                Height = (uint)(Math.Round(height / 2.0) * 2),
                Subtype = MediaEncodingSubtypes.H264,
                Bitrate = bitrate
            });

            var audio = new AudioStreamDescriptor(new AudioEncodingProperties()
            {
                Bitrate = 128,
                BitsPerSample = 16,
                ChannelCount = 2,
                SampleRate = 48000,
                Subtype = MediaEncodingSubtypes.Aac
            });

            profile.SetVideoTracks(new[] { video });
            profile.SetAudioTracks(new[] { audio });

            return await TryTranscodeMediaAsync(storageFile, stream, profile, progress, token);
        }

        public Task<bool> TryTranscodeAudioAsync(IStorageFile storageFile, Stream stream, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            if (!IsTranscodingAvailable)
                return Task.FromResult(false);

            var profile = MediaEncodingProfile.CreateMp3(hq ? AudioEncodingQuality.High : AudioEncodingQuality.Medium);
            return TryTranscodeMediaAsync(storageFile, stream, profile, progress, token);
        }

        public async Task<bool> TryTranscodeMediaAsync(IStorageFile file, Stream stream, MediaEncodingProfile profile, IProgress<double?> progress, CancellationToken token = default)
        {
            using (var fileStream = await file.OpenReadAsync())
            {
                var prep = await _transcoder.PrepareStreamTranscodeAsync(fileStream, stream.AsRandomAccessStream(), profile);

                if (prep.CanTranscode)
                {
                    var task = prep.TranscodeAsync();

                    token.Register(() => task.Cancel());
                    task.Progress = new AsyncActionProgressHandler<double>((a, p) => progress.Report(p));

                    await task;

                    return task.Status == AsyncStatus.Completed;
                }

            }

            return false;
        }

        async Task<bool> IMediaAbstractions.TryTranscodeVideoAsync(string file, Stream stream, bool hq, IProgress<double?> progress)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file);
                return await TryTranscodeVideoAsync(storageFile, stream, hq, progress);
            }
            catch { }

            return false;
        }

        async Task<bool> IMediaAbstractions.TryTranscodeAudioAsync(string file, Stream stream, bool hq, IProgress<double?> progress)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file);
                return await TryTranscodeAudioAsync(storageFile, stream, hq, progress);
            }
            catch { }

            return false;
        }

        Task<bool> IMediaAbstractions.TryTranscodeImageAsync(string file, Stream stream, bool hq, IProgress<double?> progress)
        {
            return Task.FromResult(false);
        }
    }
}

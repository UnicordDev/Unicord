using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Abstractions;
using Unicord.Universal;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.System.Profile;
using static Unicord.Constants;

namespace Unicord.Abstractions
{
    internal class UwpMediaAbstractions
    {
        public UwpMediaAbstractions()
        {
            Transcoder = new MediaTranscoder() { VideoProcessingAlgorithm = App.RoamingSettings.Read(VIDEO_PROCESSING, MediaVideoProcessingAlgorithm.MrfCrf444) };
        }

        public bool IsTranscodingAvailable => Transcoder != null;

        public MediaTranscoder Transcoder { get; set; }

        public async Task<string> GetFileMimeAsync(string path)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            return file.ContentType;
        }

        public async Task<bool> TryTranscodeVideoAsync(IStorageFile storageFile, IStorageFile output, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            if (!IsTranscodingAvailable)
                return false;

            var props = await (storageFile as IStorageItemProperties).Properties.GetVideoPropertiesAsync();

            var width = (int)props.Width;
            var height = (int)props.Height;
            var bitrate = hq ? (uint)2_000_000 : 1_115_000;

            var maxWidth = App.RoamingSettings.Read(VIDEO_WIDTH, 854);
            var maxHeight = App.RoamingSettings.Read(VIDEO_HEIGHT, 480);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            bitrate = App.RoamingSettings.Read(VIDEO_BITRATE, bitrate);

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
                Bitrate = App.RoamingSettings.Read(AUDIO_BITRATE, 192u),
                BitsPerSample = 16,
                ChannelCount = 2,
                SampleRate = App.RoamingSettings.Read(AUDIO_SAMPLERATE, 44100u),
                Subtype = MediaEncodingSubtypes.Aac
            });

            profile.SetVideoTracks(new[] { video });
            profile.SetAudioTracks(new[] { audio });

            return await TryTranscodeMediaAsync(storageFile, output, profile, progress, token);
        }

        public async Task<bool> TryTranscodeMediaAsync(IStorageFile file, IStorageFile output, MediaEncodingProfile profile, IProgress<double?> progress, CancellationToken token = default)
        {
            var prep = await Transcoder.PrepareFileTranscodeAsync(file, output, profile);

            if (prep.CanTranscode)
            {
                var task = prep.TranscodeAsync();

                token.Register(() => task.Cancel());
                task.Progress = new AsyncActionProgressHandler<double>((a, p) => progress.Report(p));

                await task;

                return task.Status == AsyncStatus.Completed;
            }

            return false;
        }

        public Task<bool> TryTranscodeAudioAsync(IStorageFile storageFile, IStorageFile output, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            if (!IsTranscodingAvailable)
                return Task.FromResult(false);

            var profile = MediaEncodingProfile.CreateMp3(hq ? AudioEncodingQuality.High : AudioEncodingQuality.Medium);
            return TryTranscodeMediaAsync(storageFile, output, profile, progress, token);
        }
    }
}

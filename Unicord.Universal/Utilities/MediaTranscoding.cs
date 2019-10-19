using System;
using System.Threading;
using System.Threading.Tasks;
using Unicord.Universal;
using WamWooWam.Core;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.FileProperties;
using static Unicord.Constants;

namespace Unicord.Universal.Utilities
{
    internal static class MediaTranscoding
    {
        public static async Task<bool> TryTranscodeVideoAsync(IStorageFile input, IStorageFile output, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            var props = await (input as IStorageItemProperties).Properties.GetVideoPropertiesAsync();
            var profile = CreateVideoEncodingProfileFromProps(hq, props);

            return await TryTranscodeMediaAsync(input, output, profile, progress, token);
        }

        public static Task<bool> TryTranscodeAudioAsync(IStorageFile input, IStorageFile output, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            var profile = MediaEncodingProfile.CreateMp3(hq ? AudioEncodingQuality.High : AudioEncodingQuality.Medium);
            return TryTranscodeMediaAsync(input, output, profile, progress, token);
        }

        public static async Task<bool> TryTranscodePhotoAsync(IStorageFile input, IStorageFile output, IProgress<double?> progress, CancellationToken token = default)
        {
            progress.Report(null);

            try
            {
                using (var inputStream = await input.OpenAsync(FileAccessMode.Read))
                using (var outputStream = await output.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(inputStream);
                    var width = (int)decoder.PixelWidth;
                    var height = (int)decoder.PixelHeight;

                    Drawing.ScaleProportions(ref width, ref height, 2048, 2048);

                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
                    encoder.BitmapTransform.ScaledWidth = (uint)width;
                    encoder.BitmapTransform.ScaledHeight = (uint)height;
                    await encoder.FlushAsync();

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> TryTranscodeMediaAsync(IStorageFile input, IStorageFile output, MediaEncodingProfile profile, IProgress<double?> progress, CancellationToken token = default)
        {
            var transcoder = new MediaTranscoder() { VideoProcessingAlgorithm = App.RoamingSettings.Read(VIDEO_PROCESSING, MediaVideoProcessingAlgorithm.Default) };
            var prep = await transcoder.PrepareFileTranscodeAsync(input, output, profile);

            if (prep.CanTranscode)
            {
                var task = prep.TranscodeAsync();

                token.Register(() => task.Cancel());
                task.Progress = new AsyncActionProgressHandler<double>((a, p) => progress.Report(p));
                await task;

                return task.Status == AsyncStatus.Completed;
            }

            transcoder = null;

            return false;
        }

        public static MediaEncodingProfile CreateVideoEncodingProfileFromProps(bool hq, VideoProperties props)
        {
            var width = (int)props.Width;
            var height = (int)props.Height;
            var bitrate = hq ? (uint)2_000_000 : 1_115_000;

            var maxWidth = App.RoamingSettings.Read(VIDEO_WIDTH, 854);
            var maxHeight = App.RoamingSettings.Read(VIDEO_HEIGHT, 480);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            bitrate = App.RoamingSettings.Read(VIDEO_BITRATE, bitrate);

            if (width == 0)
                width = maxWidth;
            if (height == 0)
                height = maxHeight;

            var profile = new MediaEncodingProfile()
            {
                Container = new ContainerEncodingProperties()
                {
                    Subtype = MediaEncodingSubtypes.Mpeg4
                },

                Video = new VideoEncodingProperties()
                {
                    Width = (uint)(Math.Round(width / 2.0) * 2),
                    Height = (uint)(Math.Round(height / 2.0) * 2),
                    Subtype = MediaEncodingSubtypes.H264,
                    Bitrate = bitrate
                },

                Audio = new AudioEncodingProperties()
                {
                    Bitrate = App.RoamingSettings.Read(AUDIO_BITRATE, 192u),
                    BitsPerSample = 16,
                    ChannelCount = 2,
                    SampleRate = App.RoamingSettings.Read(AUDIO_SAMPLERATE, 44100u),
                    Subtype = MediaEncodingSubtypes.Aac
                }
            };

            return profile;
        }
    }
}

using Microsoft.AppCenter.Analytics;
using System;
using System.Linq;
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
        public static async Task<bool> TryTranscodeVideoAsync(IStorageFile input, IStorageFile output, IProgress<double?> progress, CancellationToken token = default)
        {
            try
            {
                Analytics.TrackEvent("MediaTranscoding_VideoTranscodeRequested");

                var props = await (input as IStorageItemProperties).Properties.GetVideoPropertiesAsync();
                var profile = CreateVideoEncodingProfileFromProps(props);

                return await TryTranscodeMediaAsync(input, output, profile, progress, token);
            }
            finally
            {
                Analytics.TrackEvent("MediaTranscoding_VideoTranscodeComplete");
            }
        }

        public static Task<bool> TryTranscodeAudioAsync(IStorageFile input, IStorageFile output, bool hq, IProgress<double?> progress, CancellationToken token = default)
        {
            try
            {
                Analytics.TrackEvent("MediaTranscoding_AudioTranscodeRequested");

                var profile = MediaEncodingProfile.CreateMp3(hq ? AudioEncodingQuality.High : AudioEncodingQuality.Medium);
                return TryTranscodeMediaAsync(input, output, profile, progress, token);
            }
            finally
            {
                Analytics.TrackEvent("MediaTranscoding_AudioTranscodeComplete");
            }
        }

        public static async Task<bool> TryTranscodePhotoAsync(IStorageFile input, IStorageFile output, IProgress<double?> progress, CancellationToken token = default)
        {
            try
            {
                Analytics.TrackEvent("MediaTranscoding_PhotoTranscodeRequested");

                progress.Report(null);

                try
                {
                    using (var inputStream = await input.OpenAsync(FileAccessMode.Read))
                    using (var outputStream = await output.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var decoder = await BitmapDecoder.CreateAsync(inputStream);
                        double width = (int)decoder.PixelWidth;
                        double height = (int)decoder.PixelHeight;

                        Drawing.ScaleProportions(ref width, ref height, 4096, 4096);

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
            finally
            {
                Analytics.TrackEvent("MediaTranscoding_PhotoTranscodeComplete");
            }
        }

        public static async Task<bool> TryTranscodeMediaAsync(IStorageFile input, IStorageFile output, MediaEncodingProfile profile, IProgress<double?> progress, CancellationToken token = default)
        {
            var transcoder = new MediaTranscoder() { VideoProcessingAlgorithm = App.RoamingSettings.Read(VIDEO_PROCESSING, MediaVideoProcessingAlgorithm.Default) };
            var prep = await transcoder.PrepareFileTranscodeAsync(input, output, profile);

            if (prep.CanTranscode)
            {
                await prep.TranscodeAsync()
                          .AsTask(token, new Progress<double>((d) => progress?.Report(d)));
                return true;
            }

            transcoder = null;

            return false;
        }

        public static MediaEncodingProfile CreateVideoEncodingProfileFromProps(VideoProperties props)
        {
            var width = (double)props.Width;
            var height = (double)props.Height;

            double maxWidth = App.RoamingSettings.Read(VIDEO_WIDTH, DEFAULT_VIDEO_WIDTH);
            double maxHeight = App.RoamingSettings.Read(VIDEO_HEIGHT, DEFAULT_VIDEO_HEIGHT);

            Drawing.ScaleProportions(ref width, ref height, maxWidth, maxHeight);
            var bitrate = App.RoamingSettings.Read(VIDEO_BITRATE, DEFAULT_VIDEO_BITRATE);

            if (width == 0)
                width = maxWidth;
            if (height == 0)
                height = maxHeight;

            var profile = new MediaEncodingProfile()
            {
                Container = new ContainerEncodingProperties() { Subtype = MediaEncodingSubtypes.Mpeg4 },
                Video = new VideoEncodingProperties()
                {
                    Width = (uint)(Math.Round(width / 2.0) * 2),
                    Height = (uint)(Math.Round(height / 2.0) * 2),
                    Subtype = MediaEncodingSubtypes.H264,
                    Bitrate = bitrate
                },
                Audio = new AudioEncodingProperties()
                {
                    Bitrate = App.RoamingSettings.Read(AUDIO_BITRATE, DEFAULT_AUDIO_BITRATE),
                    BitsPerSample = 16,
                    ChannelCount = 2,
                    SampleRate = App.RoamingSettings.Read(AUDIO_SAMPLERATE, DEFAULT_AUDIO_SAMPLERATE),
                    Subtype = MediaEncodingSubtypes.Aac
                }
            };

            return profile;
        }
    }
}

using Concentus.Enums;
using DSharpPlus.VoiceNext.Codec;

namespace DSharpPlus.VoiceNext
{
    /// <summary>
    /// VoiceNext client configuration.
    /// </summary>
    public sealed class VoiceNextConfiguration
    {
        /// <summary>
        /// <para>Sets the encoding settings for this client. This decides whether the encoder will favour quality or smaller bandwidth.</para>
        /// <para>Defaults to <see cref="VoiceApplication.Music"/>.</para>
        /// </summary>
        public OpusApplication VoiceApplication { internal get; set; } = OpusApplication.OPUS_APPLICATION_AUDIO;

        /// <summary>
        /// <para>Sets whether incoming voice receiver should be enabled.</para>
        /// <para>Defaults to false.</para>
        /// </summary>
        public bool EnableIncoming { internal get; set; } = false;

        /// <summary>
        /// Creates a new instance of <see cref="VoiceNextConfiguration"/>.
        /// </summary>
        public VoiceNextConfiguration() { }

        /// <summary>
        /// Creates a new instance of <see cref="VoiceNextConfiguration"/>, copying the properties of another configuration.
        /// </summary>
        /// <param name="other">Configuration the properties of which are to be copied.</param>
        public VoiceNextConfiguration(VoiceNextConfiguration other)
        {
            VoiceApplication = other.VoiceApplication;
            EnableIncoming = other.EnableIncoming;
        }
    }
}

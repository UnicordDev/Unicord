#if !NETSTANDARD1_1
using System;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext.Codec;

namespace DSharpPlus.VoiceNext.Entities
{
    internal class AudioSender : IDisposable
    {
        public uint SSRC { get; }
        public ulong Id => UserId;
        public OpusDecoder Decoder { get; }
        public ulong UserId { get; set; }
        public ushort LastSequence { get; set; } = 0;

        public AudioSender(uint ssrc, OpusDecoder decoder)
        {
            SSRC = ssrc;
            Decoder = decoder;
        }

        public void Dispose()
        {
            Decoder?.Dispose();
        }
    }
}
#endif
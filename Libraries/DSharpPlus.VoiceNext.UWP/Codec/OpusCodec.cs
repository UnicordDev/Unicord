using Concentus.Enums;
using Concentus.Structs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DSharpPlus.VoiceNext.Codec
{
    internal static class OpusCodec
    {
        private const int PCM_SAMPLE_SIZE = 3840;

        static OpusCodec()
        {
        }

        public static byte[] Encode(byte[] pcmData, int offset, int count, OpusEncoder encoder, int bitRate = 16)
        {
            var frame = new byte[count];
            Array.Copy(pcmData, offset, frame, 0, frame.Length);

            var frame_size = FrameCount(frame.Length, bitRate);
            var enc = new byte[frame.Length];
            var len = 0;

            var sdata = new short[(int)Math.Ceiling(pcmData.Length / 2d)];
            Buffer.BlockCopy(pcmData, 0, sdata, 0, pcmData.Length);

            len = encoder.Encode(sdata, offset, frame_size, enc, 0, enc.Length);

            if (len < 0)
                throw new Exception(string.Concat("OPUS encoding failed (", len, ")"));

            Array.Resize(ref enc, len);
            return enc;
        }

        public static short[] Decode(byte[] opusData, int offset, int count, OpusDecoder decoder, ref int length,  int bitRate = 16)
        {
            length = OpusPacketInfo.GetNumSamples(opusData, offset, count, 48000);
            var frame_size = OpusPacketInfo.GetNumSamplesPerFrame(opusData, offset, 48000);

            var frame = new short[length* 2];

            var len = decoder.Decode(opusData, offset, count, frame, 0, frame_size, false);
            Array.Resize(ref frame, len);

            return frame;
        }

        private static int FrameCount(int length, int bitRate)
        {
            var bps = (bitRate >> 2) & ~1;
            return length / bps;
        }
    }
}

#pragma once

#include "SodiumWrapper.h"

namespace winrt::Unicord::Universal::Voice::Interop
{
    class Rtp
    {
    public:
        static const int HEADER_SIZE = 12;

        static void EncodeHeader(uint16_t sequence, uint32_t timestamp, uint32_t ssrc, gsl::span<uint8_t> target);

        static bool IsRtpHeader(array_view<const uint8_t> data);
        static void DecodeHeader(array_view<const uint8_t> source, uint16_t& sequence, uint32_t& timestamp, uint32_t& ssrc, bool& has_extension);
        static void GetDataFromPacket(array_view<const uint8_t> source, array_view<const uint8_t>& data, EncryptionMode mode);

        static int CalculatePacketSize(uint32_t encrypted_length, EncryptionMode encryption_mode);

    private:
        static const uint8_t RTP_NO_EXTENSION = 0x80;
        static const uint8_t RTP_EXTENSION = 0x90;
        static const uint8_t RTP_VERSION = 0x78;
    };
}


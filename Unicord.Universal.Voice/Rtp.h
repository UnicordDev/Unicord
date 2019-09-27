#pragma once

namespace winrt::Unicord::Universal::Voice::Transport
{
    enum EncryptionMode;

    struct RtpHeader {
        uint8_t type = 0;
        uint16_t seq = 0;
        uint32_t timestamp = 0;
        uint32_t ssrc = 0;
        bool extension = false;
        bool marker = false;
        uint8_t csrcs = 0;
        std::vector<uint32_t> contributing_ssrcs;

        inline size_t size() const noexcept {
            return 12 + (contributing_ssrcs.size() * 4);
        }
    };

    class Rtp
    {
    public:
        static const uint8_t RTP_NO_EXTENSION = 0x80;
        static const uint8_t RTP_EXTENSION = 0x90;
        static const uint8_t RTP_TYPE_OPUS = 0x78;
        static const uint8_t RTP_TYPE_H264 = 101;
        static const uint8_t RTP_TYPE_H264_RTX = 102;

        static bool IsRtpHeader(array_view<const uint8_t> data);
        static void EncodeHeader(const RtpHeader& header, gsl::span<uint8_t> target);
        static void DecodeHeader(array_view<const uint8_t> source, RtpHeader& header);
        static void GetDataFromPacket(array_view<const uint8_t> source, array_view<const uint8_t>& data, const RtpHeader& header, EncryptionMode mode);

        static size_t CalculatePacketSize(uint32_t encrypted_length, const RtpHeader& header, EncryptionMode encryption_mode);
    };
}


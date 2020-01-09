#pragma once
#include "SodiumWrapper.h"

namespace winrt::Unicord::Universal::Voice::Transport
{
    struct RtpPacket {
        uint8_t type = 0;
        uint16_t seq = 0;
        uint32_t timestamp = 0;
        uint32_t ssrc = 0;
        bool extension = false;
        bool marker = false;
        uint8_t csrcs = 0;
        std::vector<uint32_t> contributing_ssrcs;
        std::vector<uint8_t> data;
        std::vector<uint8_t> nonce;

        inline size_t header_size() const noexcept {
            return 12 + (contributing_ssrcs.size() * 4);
        }
    };

    class Rtp
    {
    private:
        std::vector<uint8_t> _key;
        EncryptionMode _mode;
        SodiumWrapper* _sodium;

    public:
        static const uint8_t RTP_NO_EXTENSION = 0x80;
        static const uint8_t RTP_EXTENSION = 0x90;

        static const uint8_t RTP_TYPE_OPUS = 120;
        static const uint8_t RTP_TYPE_H264 = 101;
        static const uint8_t RTP_TYPE_H264_RTX = 102;

        static bool IsRtpHeader(array_view<uint8_t> data);

        Rtp(array_view<uint8_t> key, EncryptionMode mode);

        void Read(array_view<uint8_t> source, RtpPacket& header);
        void Write(const RtpPacket& header, gsl::span<uint8_t> target);
        size_t CalculatePacketSize(uint32_t encrypted_length, const RtpPacket& header, EncryptionMode encryption_mode);
    };
}


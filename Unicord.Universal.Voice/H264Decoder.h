#pragma once
#include "Rtp.h"
#include <concurrent_unordered_map.h>

using namespace winrt::Unicord::Universal::Voice::Transport;

namespace winrt::Unicord::Universal::Voice::Decode
{
    struct H264Packet {
        RtpHeader header;
        std::vector<uint8_t> data;
    };

    struct H264Frame {
        std::vector<uint8_t> sps;
        std::vector<uint8_t> pps;
        std::vector<std::vector<uint8_t>> data;
    };

    class H264Decoder
    {
    public:
        bool ProcessPacket(const RtpHeader& header, array_view<uint8_t>& decrypted_view, H264Frame& frame);

    private:
        concurrency::concurrent_unordered_map<uint32_t, std::vector<H264Packet>> packetQueue;
    };
}
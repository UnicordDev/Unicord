#pragma once
#include "Rtp.h"
#include "H264Utils.h"
#include <concurrent_unordered_map.h>

using namespace winrt::Unicord::Universal::Voice::Utilities;
using namespace winrt::Unicord::Universal::Voice::Transport;

namespace winrt::Unicord::Universal::Voice::Decode
{
    class H264Decoder
    {
    public:
        bool ProcessPacket(const RtpHeader& header, array_view<uint8_t>& decrypted_view, H264Frame& frame);

    private:
        concurrency::concurrent_unordered_map<uint32_t, std::vector<H264Packet>> packetQueue;
    };
}
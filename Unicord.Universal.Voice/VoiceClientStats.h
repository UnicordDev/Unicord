#pragma once
#include "VoiceClientStats.g.h"

namespace winrt::Unicord::Universal::Voice::implementation {
    struct VoiceClientStats : VoiceClientStatsT<VoiceClientStats> {
        VoiceClientStats() = default;

        uint32_t LocalSSRC();
        int64_t BytesSent();
        int32_t PacketsSent();
        int32_t PacketsLost();
        hstring CodecName();
    };
}
namespace winrt::Unicord::Universal::Voice::factory_implementation {
    struct VoiceClientStats : VoiceClientStatsT<VoiceClientStats, implementation::VoiceClientStats> {
    };
}

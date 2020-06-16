#pragma once

#include "VoiceClientStats.g.h"

namespace winrt::Unicord::Universal::Voice::implementation
{
    struct VoiceClientStats : VoiceClientStatsT<VoiceClientStats>
    {
        VoiceClientStats() = default;

        int32_t MyProperty();
        void MyProperty(int32_t value);
    };
}

namespace winrt::Unicord::Universal::Voice::factory_implementation
{
    struct VoiceClientStats : VoiceClientStatsT<VoiceClientStats, implementation::VoiceClientStats>
    {
    };
}

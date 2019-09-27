#pragma once
#include "VideoEventArgs.g.h"

namespace winrt::Unicord::Universal::Voice::implementation
{
    struct VideoEventArgs : VideoEventArgsT<VideoEventArgs>
    {
        array_view<uint8_t> data;
        uint32_t ssrc;

        VideoEventArgs() = default;

        com_array<uint8_t> Data();
        uint32_t SSRC() { return ssrc; }
        void Close();
    };
}

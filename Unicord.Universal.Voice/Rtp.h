#pragma once

#include "SodiumWrapper.h"

namespace winrt::Unicord::Universal::Voice::Interop {
    class Rtp {
    public:
        static const int HEADER_SIZE = 12;
        static const uint8_t RTP_TYPE_OPUS = 120;
        static const uint8_t RTP_TYPE_H264 = 101;
        static const uint8_t RTP_TYPE_H264_RTX = 102;
    };
}

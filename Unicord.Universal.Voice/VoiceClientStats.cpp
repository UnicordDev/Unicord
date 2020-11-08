#include "pch.h"
#include "VoiceClientStats.h"
#include "VoiceClientStats.g.cpp"

namespace winrt::Unicord::Universal::Voice::implementation {
    uint32_t VoiceClientStats::LocalSSRC() {
        throw hresult_not_implemented();
    }
    int64_t VoiceClientStats::BytesSent() {
        throw hresult_not_implemented();
    }
    int32_t VoiceClientStats::PacketsSent() {
        throw hresult_not_implemented();
    }
    int32_t VoiceClientStats::PacketsLost() {
        throw hresult_not_implemented();
    }
    hstring VoiceClientStats::CodecName() {
        throw hresult_not_implemented();
    }
}

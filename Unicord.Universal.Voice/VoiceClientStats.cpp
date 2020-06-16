#include "pch.h"
#include "VoiceClientStats.h"
#if __has_include("VoiceClientStats.g.cpp")
#include "VoiceClientStats.g.cpp"
#endif

namespace winrt::Unicord::Universal::Voice::implementation
{
    int32_t VoiceClientStats::MyProperty()
    {
        throw hresult_not_implemented();
    }

    void VoiceClientStats::MyProperty(int32_t /*value*/)
    {
        throw hresult_not_implemented();
    }
}

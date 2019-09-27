#include "pch.h"
#include "VideoEventArgs.h"
#include "VideoEventArgs.g.cpp"

namespace winrt::Unicord::Universal::Voice::implementation
{
    com_array<uint8_t> VideoEventArgs::Data()
    {
        return com_array<uint8_t>(data.begin(), data.end());
    }

    void VideoEventArgs::Close()
    {
        
    }
}

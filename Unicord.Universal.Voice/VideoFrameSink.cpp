#include "pch.h"
#include "VideoFrameSink.h"
#include <iostream>

namespace winrt::Unicord::Universal::Voice::Render {
    void VideoFrameSink::OnFrame(const webrtc::VideoFrame& frame) {
        std::cout << "frame from " << videoSSRC << " w: " << frame.width() << " h: " << frame.height() << std::endl;
    }
}
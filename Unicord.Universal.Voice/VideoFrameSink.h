#pragma once
#include <api/video/video_frame.h>
#include <api/video/video_sink_interface.h>

namespace winrt::Unicord::Universal::Voice::Render {
    class VideoFrameSink : public rtc::VideoSinkInterface<webrtc::VideoFrame> {
    public:
        VideoFrameSink(uint32_t ssrc) : videoSSRC(ssrc) {
        }

        void OnFrame(const webrtc::VideoFrame& frame);

    private:
        uint32_t videoSSRC;
    };
}
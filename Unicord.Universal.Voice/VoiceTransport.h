#pragma once
#include "SodiumWrapper.h"
#include <api/call/transport.h>

using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Unicord::Universal::Voice::Interop;

namespace winrt::Unicord::Universal::Voice::Transport {
    class VoiceOutboundTransport : public webrtc::Transport {
    public:
        VoiceOutboundTransport(std::shared_ptr<SodiumWrapper> wrapper, DataWriter writer) : _isActive(false), _sodium(wrapper), _writer(writer) {
        }

        static const size_t NonceBytes = crypto_box_NONCEBYTES;

        bool SendRtp(const uint8_t* packet, size_t length, const webrtc::PacketOptions& options) override;
        bool SendRtcp(const uint8_t* packet, size_t length) override;

        void Start();
        void Stop();

    private:
        std::shared_ptr<SodiumWrapper> _sodium = nullptr;
        DataWriter _writer{ nullptr };
        bool _isActive = false;
        bool _stopping = false;
        uint32_t _rtpNonce = 0;
        uint32_t _rtcpNonce = 0;
    };
}
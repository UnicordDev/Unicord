#pragma once
#include <api/call/transport.h>
#include "SodiumWrapper.h"

using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Unicord::Universal::Voice::Interop;

namespace winrt::Unicord::Universal::Voice::Transport {
    class VoiceOutboundTransport : public webrtc::Transport {
    public:
        VoiceOutboundTransport(std::shared_ptr<SodiumWrapper> wrapper, DataWriter writer) :
            _isActive(false), _sodium(wrapper), _writer(writer) {

        }

        static const size_t NonceBytes = crypto_box_NONCEBYTES;

        virtual bool SendRtp(const uint8_t* packet, size_t length, const webrtc::PacketOptions& options);
        virtual bool SendRtcp(const uint8_t* packet, size_t length);

        void Start();
        void Stop();

    private:
        std::shared_ptr<SodiumWrapper> _sodium = nullptr;
        DataWriter _writer{ nullptr };
        bool _isActive = false;
        uint32_t _nonce = 0;
    };
}
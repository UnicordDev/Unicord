#include "pch.h"
#include "VoiceTransport.h"
#include "Rtp.h"

namespace winrt::Unicord::Universal::Voice::Transport {

    void VoiceOutboundTransport::Start() {
        _isActive = true;
    }

    bool VoiceOutboundTransport::SendRtp(const uint8_t* packet, size_t length, const webrtc::PacketOptions& options) {
        static const size_t RtpHeaderSize = 12;

        if (!_isActive) {
            return true;
        }

        size_t encrypted_size = _sodium->CalculateTargetSize(length);
        gsl::span<const uint8_t> packet_span(packet, length);
        std::vector<uint8_t> new_packet(encrypted_size);

        uint8_t packet_nonce[NonceBytes] = { 0 };
        switch (_sodium->GetCurrentEncryptionMode()) {
        case EncryptionMode::XSalsa20_Poly1305:
            _sodium->GenerateNonce(packet_span.subspan(0, RtpHeaderSize), packet_nonce);
            break;
        case EncryptionMode::XSalsa20_Poly1305_Suffix:
            _sodium->GenerateNonce(packet_nonce);
            break;
        case EncryptionMode::XSalsa20_Poly1305_Lite:
            _sodium->GenerateNonce(this->_rtpNonce++, packet_nonce);
            break;
        }

        std::copy(packet, packet + RtpHeaderSize, new_packet.data());
        _sodium->Encrypt(packet_span.subspan(RtpHeaderSize), packet_nonce, gsl::make_span(new_packet).subspan(RtpHeaderSize));
        _sodium->AppendNonce(packet_nonce, new_packet);

        _writer.WriteBytes(new_packet);
        _writer.StoreAsync().get();

        if (_stopping) {
            _stopping = false;
            _isActive = false;
        }

        return true;
    }

    bool VoiceOutboundTransport::SendRtcp(const uint8_t* packet, size_t length) {
        static const size_t RtcpHeaderSize = 8;

        uint8_t packet_nonce[NonceBytes]{ 0 };

        size_t encrypted_size = _sodium->CalculateTargetSize(length);
        gsl::span<const uint8_t> packet_span(packet, length);
        std::vector<uint8_t> new_packet(encrypted_size);

        switch (_sodium->GetCurrentEncryptionMode()) {
        case EncryptionMode::XSalsa20_Poly1305:
            _sodium->GenerateNonce(packet_span.subspan(0, RtcpHeaderSize), packet_nonce);
            break;
        case EncryptionMode::XSalsa20_Poly1305_Suffix:
            _sodium->GenerateNonce(packet_nonce);
            break;
        case EncryptionMode::XSalsa20_Poly1305_Lite:
            _sodium->GenerateNonce(this->_rtcpNonce++, packet_nonce);
            break;
        }

        std::copy(packet, packet + RtcpHeaderSize, new_packet.data());
        _sodium->Encrypt(packet_span.subspan(RtcpHeaderSize), packet_nonce, gsl::make_span(new_packet).subspan(RtcpHeaderSize));
        _sodium->AppendNonce(packet_nonce, new_packet);

        _writer.WriteBytes(new_packet);
        _writer.StoreAsync().get();

        return true;
    }

    void VoiceOutboundTransport::Stop() {
        _stopping = true;
    }
}
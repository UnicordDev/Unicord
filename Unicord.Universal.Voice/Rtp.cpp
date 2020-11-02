#include "pch.h"
#include "Rtp.h"
#include <sodium.h>

namespace winrt::Unicord::Universal::Voice::Interop
{
    void Rtp::EncodeHeader(uint16_t sequence, uint32_t timestamp, uint32_t ssrc, gsl::span<uint8_t> target)
    {
        if (target.size() < HEADER_SIZE) {
            throw hresult_invalid_argument();
        }

        target[0] = RTP_NO_EXTENSION;
        target[1] = RTP_VERSION;

        // reverse_copy from big endian to little endian 
        std::reverse_copy((uint8_t*)&sequence, (uint8_t*)&sequence + sizeof sequence, &target[2]);
        std::reverse_copy((uint8_t*)&timestamp, (uint8_t*)&timestamp + sizeof timestamp, &target[4]);
        std::reverse_copy((uint8_t*)&ssrc, (uint8_t*)&ssrc + sizeof ssrc, &target[8]);
    }

    bool Rtp::IsRtpHeader(array_view<const uint8_t> data)
    {
        if (data.size() < HEADER_SIZE)
            return false;

        if ((data[0] != RTP_NO_EXTENSION && data[0] != RTP_EXTENSION) || data[1] != RTP_VERSION)
            return false;

        return true;
    }

    void Rtp::DecodeHeader(array_view<const uint8_t> source, uint16_t& sequence, uint32_t& timestamp, uint32_t& ssrc, bool& has_extension)
    {
        if (!IsRtpHeader(source))
            throw hresult_invalid_argument();

        has_extension = source[0] == RTP_EXTENSION;

        // reverse_copy from big endian to little endian 
        std::reverse_copy(&source[2], &source[2 + sizeof sequence], (uint8_t*)&sequence);
        std::reverse_copy(&source[4], &source[4 + sizeof timestamp], (uint8_t*)&timestamp);
        std::reverse_copy(&source[8], &source[8 + sizeof ssrc], (uint8_t*)&ssrc);
    }

    void Rtp::GetDataFromPacket(array_view<const uint8_t> source, array_view<const uint8_t> &destination, EncryptionMode mode)
    {
        switch (mode)
        {
        case XSalsa20_Poly1305:
            destination = array_view(source.begin() + HEADER_SIZE, source.end());
            break;
        case XSalsa20_Poly1305_Suffix:
            destination = array_view(source.begin() + HEADER_SIZE, source.end() - crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
            break;
        case XSalsa20_Poly1305_Lite:
            destination = array_view(source.begin() + HEADER_SIZE, source.end() - 4);
            break;
        default:
            throw hresult_invalid_argument();
        }
    }

    int Rtp::CalculatePacketSize(uint32_t encrypted_length, EncryptionMode encryption_mode)
    {
    }
}
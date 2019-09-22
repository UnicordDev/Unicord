#include "pch.h"
#include "Rtp.h"
#include "SodiumWrapper.h"
#include <sodium.h>

namespace winrt::Unicord::Universal::Voice::Interop
{
    bool Rtp::IsRtpHeader(array_view<const uint8_t> data)
    {
        if (data.size() < 12)
            return false;

        uint8_t header_magic = (data[0] & 0b11110000);
        if ((header_magic != RTP_NO_EXTENSION && header_magic != RTP_EXTENSION))
            return false;

        return true;
    }

    void Rtp::EncodeHeader(const RtpHeader& header, gsl::span<uint8_t> target)
    {
        if (target.size() < header.size()) {
            throw hresult_invalid_argument();
        }

        target[0] = (header.extension ? RTP_EXTENSION : RTP_NO_EXTENSION) | header.contributing_ssrcs.size();
        target[1] = header.type;

        // reverse_copy from big endian to little endian 
        std::reverse_copy((uint8_t*)&header.seq, (uint8_t*)&header.seq + sizeof header.seq, &target[2]);
        std::reverse_copy((uint8_t*)&header.timestamp, (uint8_t*)&header.timestamp + sizeof header.timestamp, &target[4]);
        std::reverse_copy((uint8_t*)&header.ssrc, (uint8_t*)&header.ssrc + sizeof header.ssrc, &target[8]);

        for (uint8_t i = 0; i < header.contributing_ssrcs.size(); i++)
        {
            uint32_t ssrc = header.contributing_ssrcs[i];
            std::reverse_copy((uint8_t*)&ssrc, (uint8_t*)&ssrc + sizeof ssrc, &target[12 + (4 * i)]);
        }
    }

    void Rtp::DecodeHeader(array_view<const uint8_t> source, RtpHeader& header)
    {
        if (!IsRtpHeader(source)) {
            throw hresult_invalid_argument();
        }

        header.type = source[1] & 0b01111111;
        header.extension = (source[0] & 0b11110000) == RTP_EXTENSION;

        // reverse_copy from big endian to little endian 
        std::reverse_copy(&source[2], &source[2 + sizeof header.seq], (uint8_t*)&header.seq);
        std::reverse_copy(&source[4], &source[4 + sizeof header.timestamp], (uint8_t*)&header.timestamp);
        std::reverse_copy(&source[8], &source[8 + sizeof header.ssrc], (uint8_t*)&header.ssrc);

        uint8_t contributing_count = source[0] & 0b00001111;
        for (uint8_t i = 0; i < contributing_count; i++)
        {
            uint32_t ssrc = 0;
            std::reverse_copy(&source[12 + (4 * i)], &source[12 + (4 * i) + sizeof ssrc], (uint8_t*)&ssrc);
            header.contributing_ssrcs.push_back(ssrc);
        }
    }

    void Rtp::GetDataFromPacket(array_view<const uint8_t> source, array_view<const uint8_t> &destination, const RtpHeader& header, EncryptionMode mode)
    {
        switch (mode)
        {
        case XSalsa20_Poly1305:
            destination = array_view(source.begin() + header.size(), source.end());
            break;
        case XSalsa20_Poly1305_Suffix:
            destination = array_view(source.begin() + header.size(), source.end() - crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
            break;
        case XSalsa20_Poly1305_Lite:
            destination = array_view(source.begin() + header.size(), source.end() - 4);
            break;
        default:
            throw hresult_invalid_argument();
        }
    }

    int Rtp::CalculatePacketSize(uint32_t encrypted_length, const RtpHeader& header, EncryptionMode encryption_mode)
    {
        switch (encryption_mode)
        {
        case XSalsa20_Poly1305_Lite:
            return header.size() + encrypted_length + 4;
        case XSalsa20_Poly1305_Suffix:
            return header.size() + encrypted_length + crypto_secretbox_xsalsa20poly1305_NONCEBYTES;
        case XSalsa20_Poly1305:
            return header.size() + encrypted_length;
        default:
            throw hresult_invalid_argument();
        }
    }
}
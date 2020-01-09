#include "pch.h"
#include "Rtp.h"
#include <sodium.h>
#include <iostream>

namespace winrt::Unicord::Universal::Voice::Transport
{
    bool Rtp::IsRtpHeader(array_view<uint8_t> data)
    {
        if (data.size() < 12)
            return false;

        uint8_t version = data[0] & 0xC0;
        if (version != 2) {
            return false;
        }

        uint8_t header_magic = (data[0] & 0b11110000);
        if ((header_magic != RTP_NO_EXTENSION && header_magic != RTP_EXTENSION))
            return false;

        return true;
    }

    Rtp::Rtp(array_view<uint8_t> key, EncryptionMode mode) : _mode(mode)
    {
        _key = std::vector<uint8_t>{ key.begin(), key.end() };
        _sodium = new SodiumWrapper(_key, mode);
    }

    void Rtp::Read(array_view<uint8_t> source, RtpPacket& header)
    {
        if (!IsRtpHeader(source)) {
            throw hresult_invalid_argument();
        }

        header.type = source[1] & 0b01111111;
        header.extension = (source[0] & 0b11110000) == RTP_EXTENSION;
        header.marker = ((source[1] >> 7) & 0x01) != 0;

        // reverse_copy from big endian to little endian 
        std::reverse_copy(&source[2], &source[2 + sizeof header.seq], (uint8_t*)&header.seq);
        std::reverse_copy(&source[4], &source[4 + sizeof header.timestamp], (uint8_t*)&header.timestamp);
        std::reverse_copy(&source[8], &source[8 + sizeof header.ssrc], (uint8_t*)&header.ssrc);

        header.csrcs = (source[0] & 0x0F) >> 0;
        for (uint8_t i = 0; i < header.csrcs; i++)
        {
            uint32_t ssrc = 0;
            std::reverse_copy(&source[12 + (4 * i)], &source[12 + (4 * i) + sizeof ssrc], (uint8_t*)&ssrc);
            header.contributing_ssrcs.push_back(ssrc);
        }

        std::vector<uint8_t> encrypted_data;
        header.nonce = std::vector<uint8_t>(crypto_secretbox_xsalsa20poly1305_NONCEBYTES);

        switch (_mode)
        {
        case EncryptionMode::XSalsa20_Poly1305:
            encrypted_data = std::vector<uint8_t>(source.size() - header.header_size());
            std::copy(source.begin() + header.header_size(), source.end(), encrypted_data.begin());
            std::copy(source.begin(), source.begin() + min(header.header_size(), crypto_secretbox_xsalsa20poly1305_NONCEBYTES), header.nonce.begin());
            break;
        case EncryptionMode::XSalsa20_Poly1305_Suffix:
            encrypted_data = std::vector<uint8_t>(source.size() - header.header_size() - crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
            std::copy(source.begin() + header.header_size(), source.end() - crypto_secretbox_xsalsa20poly1305_NONCEBYTES, encrypted_data.begin());
            std::copy(source.end() - 12, source.end(), header.nonce.begin());
            break;
        case EncryptionMode::XSalsa20_Poly1305_Lite:
            encrypted_data = std::vector<uint8_t>(source.size() - header.header_size() - 4);
            std::copy(source.begin() + header.header_size(), source.end() - 4, encrypted_data.begin());
            std::copy(source.end() - 4, source.end(), header.nonce.begin());
            break;
        }

        // calculate the size of the decrypted data
        size_t decrypted_size = _sodium->CalculateSourceSize(encrypted_data.size());
        header.data = std::vector<uint8_t>(decrypted_size);
        _sodium->Decrypt(encrypted_data, header.nonce, header.data);

    }

    void Rtp::Write(const RtpPacket& header, gsl::span<uint8_t> target)
    {
        if ((size_t)target.size() < header.header_size()) {
            throw hresult_invalid_argument();
        }

        if (header.contributing_ssrcs.size() > 127) {
            throw hresult_invalid_argument();
        }

        target[0] = (header.extension ? RTP_EXTENSION : RTP_NO_EXTENSION) | (uint8_t)header.contributing_ssrcs.size();
        target[1] = header.type;

        // reverse_copy from big endian to little endian 
        std::reverse_copy((uint8_t*)&header.seq, (uint8_t*)&header.seq + sizeof header.seq, &target[2]);
        std::reverse_copy((uint8_t*)&header.timestamp, (uint8_t*)&header.timestamp + sizeof header.timestamp, &target[4]);
        std::reverse_copy((uint8_t*)&header.ssrc, (uint8_t*)&header.ssrc + sizeof header.ssrc, &target[8]);

        for (uint8_t i = 0; i < header.contributing_ssrcs.size(); i++)
        {
            uint32_t ssrc = header.contributing_ssrcs[i];
            std::reverse_copy((uint8_t*)&ssrc, (uint8_t*)&ssrc + sizeof ssrc, &target[12 + ((sizeof ssrc) * (size_t)i)]);
        }

        std::copy(header.data.begin(), header.data.end(), target.begin() + header.header_size());
    }

    size_t Rtp::CalculatePacketSize(uint32_t encrypted_length, const RtpPacket& header, EncryptionMode encryption_mode)
    {
        switch (encryption_mode)
        {
        case EncryptionMode::XSalsa20_Poly1305_Lite:
            return header.header_size() + encrypted_length + 4;
        case EncryptionMode::XSalsa20_Poly1305_Suffix:
            return header.header_size() + encrypted_length + crypto_secretbox_xsalsa20poly1305_NONCEBYTES;
        case EncryptionMode::XSalsa20_Poly1305:
            return header.header_size() + encrypted_length;
        default:
            throw hresult_invalid_argument();
        }
    }
}
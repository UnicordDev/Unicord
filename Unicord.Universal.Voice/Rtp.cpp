#include "pch.h"
#include "Rtp.h"
#include <sodium.h>

namespace winrt::Unicord::Universal::Voice::Interop
{
	void Rtp::EncodeHeader(uint16_t sequence, uint32_t timestamp, uint32_t ssrc, uint8_t target[], size_t target_size)
	{
		if (target_size < HEADER_SIZE) {
			throw hresult_invalid_argument();
		}

		target[0] = RTP_NO_EXTENSION;
		target[1] = RTP_VERSION;

		// reverse_copy from big endian to little endian 
		std::reverse_copy((uint8_t*)&sequence, (uint8_t*)&sequence + sizeof sequence, &target[2]);
		std::reverse_copy((uint8_t*)&timestamp, (uint8_t*)&timestamp + sizeof timestamp, &target[4]);
		std::reverse_copy((uint8_t*)&ssrc, (uint8_t*)&ssrc + sizeof ssrc, &target[8]);
	}

	bool Rtp::IsRtpHeader(uint8_t source[], size_t source_size)
	{
		if (source_size < HEADER_SIZE)
			return false;

		if ((source[0] != RTP_NO_EXTENSION && source[0] != RTP_EXTENSION) || source[1] != RTP_VERSION)
			return false;

		return true;
	}

	void Rtp::DecodeHeader(uint8_t source[], size_t source_size, uint16_t& sequence, uint32_t& timestamp, uint32_t& ssrc, bool& has_extension)
	{
		if (!IsRtpHeader(source, source_size))
			throw hresult_invalid_argument();

		has_extension = source[0] == RTP_EXTENSION;

		// reverse_copy from big endian to little endian 
		std::reverse_copy(&source[2], &source[2] + sizeof sequence, &sequence);
		std::reverse_copy(&source[4], &source[4] + sizeof timestamp, &timestamp);
		std::reverse_copy(&source[8], &source[8] + sizeof ssrc, &ssrc);
	}

	void Rtp::GetDataFromPacket(uint8_t source[], size_t source_size, uint8_t destination[], size_t& destination_size, EncryptionMode mode)
	{
		destination = &source[HEADER_SIZE];

		switch (mode)
		{
		case XSalsa20_Poly1305_Lite:
			destination_size = source_size - HEADER_SIZE - 4;
			break;
		case XSalsa20_Poly1305_Suffix:
			destination_size = source_size - HEADER_SIZE - crypto_secretbox_xsalsa20poly1305_NONCEBYTES;
			break;
		case XSalsa20_Poly1305:
			destination_size = source_size - HEADER_SIZE;
			break;
		default:
			throw hresult_invalid_argument();
		}
	}

	int Rtp::CalculatePacketSize(uint32_t encrypted_length, EncryptionMode encryption_mode)
	{
		switch (encryption_mode)
		{
		case XSalsa20_Poly1305_Lite:
			return HEADER_SIZE + encrypted_length + 4;
		case XSalsa20_Poly1305_Suffix:
			return HEADER_SIZE + encrypted_length + crypto_secretbox_xsalsa20poly1305_NONCEBYTES;
		case XSalsa20_Poly1305:
			return HEADER_SIZE + encrypted_length;
		default:
			throw hresult_invalid_argument();
		}
	}
}
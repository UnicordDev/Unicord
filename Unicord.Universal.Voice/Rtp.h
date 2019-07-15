#pragma once

#include "SodiumWrapper.h"

namespace winrt::Unicord::Universal::Voice::Interop
{
	class Rtp
	{
	public:
		static const int HEADER_SIZE = 12;

		static void EncodeHeader(uint16_t sequence, uint32_t timestamp, uint32_t ssrc, uint8_t target[], size_t target_size);
		static bool IsRtpHeader(uint8_t source[], size_t source_size);

		static void DecodeHeader(uint8_t source[], size_t source_size, uint16_t& sequence, uint32_t& timestamp, uint32_t& ssrc, bool& has_extension);
		static void GetDataFromPacket(uint8_t source[], size_t source_size, uint8_t destination[], size_t& destination_size, EncryptionMode mode);

		static int CalculatePacketSize(uint32_t encrypted_length, EncryptionMode encryption_mode);

	private:
		static const uint8_t RTP_NO_EXTENSION = 0x80;
		static const uint8_t RTP_EXTENSION = 0x90;
		static const uint8_t RTP_VERSION = 0x78;
	};
}


#include "pch.h"
#include "Rtp.h"

namespace winrt::Unicord::Universal::Voice::Interop
{
	void Rtp::EncodeHeader(uint16_t sequence, uint32_t timestamp, uint32_t ssrc, uint8_t target[], uint32_t target_size) 
	{
		if (target_size < HEADER_SIZE) {
			throw hresult_invalid_argument();
		}

		target[0] = RTP_NO_EXTENSION;
		target[1] = RTP_VERSION;


	}

	bool Rtp::IsRtpHeader(uint8_t source[], uint32_t source_size) 
	{
		return false;
	}

	void Rtp::DecodeHeader(uint8_t source[], uint32_t source_size, uint16_t* sequence, uint32_t* timestamp, uint32_t* ssrc, bool* has_extension) 
	{
	}

	int Rtp::CalculatePacketSize(uint32_t encrypted_length, EncryptionMode encryption_mode)
	{
		return 0;
	}
}
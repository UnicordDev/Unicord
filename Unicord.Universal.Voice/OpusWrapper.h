#pragma once
#include "AudioFormat.h"
#include <opus.h>

namespace winrt::Unicord::Universal::Voice::Interop
{
	class OpusWrapper
	{
	public:
		OpusWrapper() = default;
		OpusWrapper(AudioFormat format);

		size_t Encode(array_view<uint8_t> pcm, uint8_t target[], size_t target_offset, size_t target_size);

		~OpusWrapper();
	private:
		AudioFormat audio_format;
		OpusEncoder* opus_encoder = nullptr;
		std::vector<OpusDecoder*> opus_decoders;

		void check_opus_error(int error, winrt::hstring message);
	};
}

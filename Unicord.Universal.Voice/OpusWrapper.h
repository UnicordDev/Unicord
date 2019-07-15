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

		size_t Encode(array_view<uint8_t> pcm, array_view<uint8_t> target);
		void Decode(AudioSource decoder, array_view<uint8_t> opus, array_view<uint8_t> &target, bool fec, AudioFormat& format);
		void ProcessPacketLoss(AudioSource decoder, int32_t frameSize, array_view<uint8_t> target);

		AudioSource* GetOrCreateDecoder(uint8_t ssrc);
		int32_t GetLastPacketSampleCount(OpusDecoder* decoder);

		~OpusWrapper();
	private:
		AudioFormat audio_format;
		OpusEncoder* opus_encoder = nullptr;
		std::map<uint8_t, AudioSource*> opus_decoders;

		void check_opus_error(int error, winrt::hstring message);
	};
}

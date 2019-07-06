#pragma once
#include <stdint.h>
#include <opus.h>

namespace winrt::Unicord::Universal::Voice::Interop
{
	enum VoiceApplication 
	{
		music = OPUS_APPLICATION_AUDIO,
		voip = OPUS_APPLICATION_VOIP,
		low_latency = OPUS_APPLICATION_RESTRICTED_LOWDELAY
	};

	struct AudioFormat
	{
	public:
		AudioFormat(uint32_t sampleRate = 48000, uint32_t channelCount = 2, VoiceApplication app = voip)
		{
			sample_rate = sampleRate;
			channel_count = channelCount;
			application = app;
		}

		uint32_t sample_rate;
		uint32_t channel_count;
		VoiceApplication application;

		inline uint32_t CalculateSampleSize(uint32_t duration) {
			return duration * channel_count * (sample_rate / 2000) * sizeof(uint16_t);
		}

		inline uint32_t GetMaxBufferSize() {
			return  120 * (sample_rate / 1000);
		}

		inline uint32_t CalculateSampleDuration(uint32_t sampleSize) {
			return sampleSize / (sample_rate / 1000) / channel_count / sizeof(uint16_t);
		}

		inline uint32_t CalculateFrameSize(int sampleDuration) {
			return sampleDuration * (sample_rate / 1000);
		}

		inline uint32_t SampleCountToSampleSize(int count) {
			return count * channel_count * sizeof(uint16_t);
		}
	};
}

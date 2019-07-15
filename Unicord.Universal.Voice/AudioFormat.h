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

	struct VoicePacket
	{
	public:
		VoicePacket() = default;
		VoicePacket(array_view<const uint8_t> packet_bytes, uint32_t packet_duration, bool isSilence = false)
		{
			bytes = packet_bytes;
			duration = packet_duration;
			is_silence = isSilence;
		}

		array_view<const uint8_t> bytes;
		uint32_t length;
		uint32_t duration;
		bool is_silence;
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

		inline size_t CalculateSampleSize(uint32_t duration) {
			return duration * channel_count * (sample_rate / 1000) * 2;
		}

		inline size_t GetMaxBufferSize() {
			return  120 * (sample_rate / 1000);
		}

		inline uint32_t CalculateSampleDuration(uint32_t sampleSize) {
			return sampleSize / (sample_rate / 1000) / channel_count / 2;
		}

		inline size_t CalculateFrameSize(uint32_t sampleDuration) {
			return sampleDuration * (sample_rate / 1000);
		}

		inline size_t SampleCountToSampleSize(size_t count) {
			return count * channel_count * 2;
		}
	};
}

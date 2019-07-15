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
		VoicePacket() { }
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

		inline bool operator== (AudioFormat& lhs) {
			return (lhs.sample_rate == sample_rate) && (lhs.channel_count == channel_count) && (lhs.application == application);
		}

		inline bool operator!= (AudioFormat& lhs) {
			return !(lhs == *this);
		}

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

	struct AudioSource
	{
	public:
		AudioSource() { }
		AudioSource(uint32_t ssrc)
		{
			this->ssrc = ssrc;
		}

		inline void Initialise(AudioFormat format) {
			int error = 0;
			if (decoder == nullptr) {
				decoder = opus_decoder_create(format.sample_rate, format.channel_count, &error);
			}
			else {
				error = opus_decoder_init(decoder, format.sample_rate, format.channel_count);
			}

			this->format = format;
		}

		inline bool IsInitialised() {
			return decoder == nullptr;
		}

		uint32_t ssrc;
		uint64_t user_id;
		uint16_t seq;
		AudioFormat format;
		OpusDecoder* decoder = nullptr;
	};

}

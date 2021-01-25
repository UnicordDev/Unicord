#pragma once
#include <iostream>
#include <opus.h>
#include <stdint.h>
#include <bitset>

namespace winrt::Unicord::Universal::Voice::Interop {
    enum VoiceApplication {
        music = OPUS_APPLICATION_AUDIO,
        voip = OPUS_APPLICATION_VOIP,
        low_latency = OPUS_APPLICATION_RESTRICTED_LOWDELAY
    };

    struct VoicePacket {
    public:
        VoicePacket() {}
        VoicePacket(std::vector<uint8_t> packet_bytes, uint32_t packet_duration, bool isSilence = false) {
            bytes = packet_bytes;
            duration = packet_duration;
            is_silence = isSilence;

            std::bitset<12> set;
        }

        std::vector<uint8_t> bytes;
        uint32_t duration = 0;
        bool is_silence = false;
    };

    struct PCMPacket {
        PCMPacket() {}
        PCMPacket(gsl::span<uint8_t> packet_bytes, uint32_t packet_duration, bool isSilence = false) {
            bytes = packet_bytes;
            duration = packet_duration;
            is_silence = isSilence;
        }

        gsl::span<uint8_t> bytes;
        uint32_t duration = 0;
        bool is_float = false;
        bool is_silence = false;
    };

    struct AudioFormat {
    public:
        AudioFormat(uint32_t sampleRate = 48000, uint32_t channelCount = 2, VoiceApplication app = music) {
            sample_rate = sampleRate;
            channel_count = channelCount;
            application = app;
        }

        uint32_t sample_rate = 0;
        uint32_t channel_count = 0;
        VoiceApplication application = voip;

        inline bool operator==(AudioFormat& lhs) noexcept {
            return (lhs.sample_rate == sample_rate) && (lhs.channel_count == channel_count) && (lhs.application == application);
        }

        inline bool operator!=(AudioFormat& lhs) noexcept {
            return !(lhs == *this);
        }

        inline size_t CalculateSampleSize(uint32_t duration) noexcept {
            return duration * channel_count * (sample_rate / 1000) * 2;
        }

        inline size_t CalculateSampleSizeF(uint32_t duration) noexcept {
            return duration * channel_count * (sample_rate / 1000) * 4;
        }

        inline size_t GetMaxBufferSize() noexcept {
            return 120 * (sample_rate / 1000);
        }

        inline uint32_t CalculateSampleDuration(uint32_t sampleSize) noexcept {
            return sampleSize / (sample_rate / 1000) / channel_count / 2;
        }

        inline uint32_t CalculateSampleDurationF(uint32_t sampleSize) noexcept {
            return sampleSize / (sample_rate / 1000) / channel_count / 4;
        }

        inline size_t CalculateFrameSize(uint32_t sampleDuration) noexcept {
            return sampleDuration * (sample_rate / 1000);
        }

        inline size_t SampleCountToSampleSize(size_t count) noexcept {
            return count * channel_count * 2;
        }

        inline size_t SampleCountToSampleSizeF(size_t count) noexcept {
            return count * channel_count * 4;
        }
    };

    struct AudioSource {
    public:
        AudioSource() {}
        AudioSource(uint32_t ssrc) {
            this->ssrc = ssrc;
        }

        void Initialise(AudioFormat new_format) {
            int error = 0;

            if (decoder != nullptr) {
                opus_decoder_destroy(decoder);
            }
            this->decoder = opus_decoder_create(new_format.sample_rate, new_format.channel_count, &error);
            this->format = new_format;
        }

        inline bool IsInitialised() {
            return decoder != nullptr;
        }

        ~AudioSource() {
            std::cout << "Freeing AudioSource\n";
            opus_decoder_destroy(decoder);
            decoder = nullptr;
        }

        uint32_t ssrc = 0;
        uint64_t user_id = 0;
        uint16_t seq = 0;
        uint64_t packets_lost = 0;
        bool is_speaking = false;
        AudioFormat format;
        OpusDecoder* decoder = nullptr;
    };
}

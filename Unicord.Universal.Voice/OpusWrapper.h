#pragma once
#include "AudioFormat.h"
#include <mutex>
#include <opus.h>

namespace winrt::Unicord::Universal::Voice::Interop {
    class OpusWrapper {
    public:
        OpusWrapper() = default;
        OpusWrapper(AudioFormat format);

        size_t Encode(array_view<uint8_t> pcm, gsl::span<uint8_t> target);
        size_t EncodeFloat(array_view<uint8_t> pcm, gsl::span<uint8_t> target);
        void Decode(AudioSource* decoder, array_view<uint8_t> opus, std::vector<uint8_t>& target, bool fec);
        void ProcessPacketLoss(AudioSource* decoder, int32_t frame_size, std::vector<uint8_t>& target);

        AudioSource* GetOrCreateDecoder(uint32_t ssrc);
        int32_t GetLastPacketSampleCount(OpusDecoder* decoder);

        ~OpusWrapper();

    private:
        AudioFormat audio_format;
        std::mutex encode_mutex;
        OpusEncoder* opus_encoder = nullptr;
        std::map<uint32_t, AudioSource*> opus_decoders;

        void check_opus_error(int error, winrt::hstring message);
    };
}

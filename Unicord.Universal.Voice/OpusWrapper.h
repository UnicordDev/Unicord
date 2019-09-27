#pragma once
#include "AudioFormat.h"
#include <opus.h>
#include <mutex>

namespace winrt::Unicord::Universal::Voice::Interop
{
    class OpusWrapper
    {
    public:
        OpusWrapper() = default;
        OpusWrapper(AudioFormat format);

        size_t Encode(array_view<uint8_t> pcm, gsl::span<uint8_t> target);
        size_t EncodeFloat(array_view<uint8_t> pcm, gsl::span<uint8_t> target);

        ~OpusWrapper();
    private:
        AudioFormat audio_format;

        void check_opus_error(int error, winrt::hstring message);
    };
}

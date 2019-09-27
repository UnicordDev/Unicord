#pragma once
#include <opus.h>
#include "Rtp.h"
#include "AudioFormat.h"

using namespace winrt::Unicord::Universal::Voice::Interop;
using namespace winrt::Unicord::Universal::Voice::Transport;

namespace winrt::Unicord::Universal::Voice::Encode
{
    class OpusEncoder
    {
    public:
        OpusEncoder(const AudioFormat& format);

        size_t OpusEncoder::Encode(array_view<uint8_t> pcm, gsl::span<uint8_t> target);
        size_t OpusEncoder::EncodeFloat(array_view<uint8_t> pcm, gsl::span<uint8_t> target);

        ~OpusEncoder();

    private:
        std::mutex encode_mutex;
        AudioFormat audioFormat;
        ::OpusEncoder* encoder = nullptr;
    };
}
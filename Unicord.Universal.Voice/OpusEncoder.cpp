#include "pch.h"
#include "OpusEncoder.h"
#include "OpusUtils.h"

using namespace winrt::Unicord::Universal::Voice::Utilities;

namespace winrt::Unicord::Universal::Voice::Encode
{
    OpusEncoder::OpusEncoder(const AudioFormat& format)
    {
        this->audioFormat = format;

        int error;
        this->encoder = opus_encoder_create(format.sample_rate, format.channel_count, (int)format.application, &error);
        OpusUtils::CheckOpusError(error, L"Failed to instantate Opus encoder");

        int signal = OPUS_AUTO;
        switch (format.application)
        {
        case VoiceApplication::Voip:
            signal = OPUS_SIGNAL_VOICE;
            break;

        case VoiceApplication::Music:
            signal = OPUS_SIGNAL_MUSIC;
            break;
        }

        OpusUtils::CheckOpusError(opus_encoder_ctl(this->encoder, OPUS_SET_SIGNAL_REQUEST, signal), L"Failed to set signal.");
        OpusUtils::CheckOpusError(opus_encoder_ctl(this->encoder, OPUS_SET_PACKET_LOSS_PERC_REQUEST, 15), L"Failed to set packet loss percent.");
        OpusUtils::CheckOpusError(opus_encoder_ctl(this->encoder, OPUS_SET_INBAND_FEC_REQUEST, 1), L"Failed to set fec.");
        OpusUtils::CheckOpusError(opus_encoder_ctl(this->encoder, OPUS_SET_BITRATE_REQUEST, 131072), L"Failed to set bitrate.");
    }

    size_t OpusEncoder::Encode(array_view<uint8_t> pcm, gsl::span<uint8_t> target)
    {
        std::unique_lock lock(encode_mutex);

        try
        {
            auto duration = audioFormat.CalculateSampleDuration(pcm.size());
            auto frame_size = audioFormat.CalculateFrameSize(duration);
            auto sample_size = audioFormat.CalculateSampleSize(duration);

            if (pcm.size() != sample_size)
                throw winrt::hresult_invalid_argument(L"Invalid PCM sample size.");

            int length = opus_encode(encoder, (int16_t*)(pcm.data()), (int32_t)frame_size, target.data(), (int32_t)target.size());
            if (length < 0) {
                OpusUtils::CheckOpusError(length, L"Could not encode PCM to opus!");
            }

            return (size_t)length;
        }
        catch (winrt::hresult_invalid_argument &)
        {
            return 0;
        }
    }

    size_t OpusEncoder::EncodeFloat(array_view<uint8_t> pcm, gsl::span<uint8_t> target)
    {
        std::unique_lock lock(encode_mutex);

        try
        {
            auto duration = audioFormat.CalculateSampleDurationF(pcm.size());
            auto frame_size = audioFormat.CalculateFrameSize(duration);
            auto sample_size = audioFormat.CalculateSampleSizeF(duration);

            if (pcm.size() != sample_size)
                throw winrt::hresult_invalid_argument(L"Invalid PCM sample size.");

            int length = opus_encode_float(encoder, (float*)(pcm.data()), (int32_t)frame_size, target.data(), (int32_t)target.size());
            if (length < 0) {
                OpusUtils::CheckOpusError(length, L"Could not encode PCM to opus!");
            }

            return (size_t)length;
        }
        catch (winrt::hresult_invalid_argument &)
        {
            return 0;
        }
    }

    OpusEncoder::~OpusEncoder()
    {
        if (this->encoder != nullptr)
        {
            opus_encoder_destroy(this->encoder);
        }
    }
}
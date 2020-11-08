#include "pch.h"
#include "OpusWrapper.h"
#include <iostream>

namespace winrt::Unicord::Universal::Voice::Interop {
    OpusWrapper::OpusWrapper(AudioFormat format) {
        int error;
        this->audio_format = format;
        this->opus_encoder = opus_encoder_create(format.sample_rate, format.channel_count, (int)format.application, &error);
        check_opus_error(error, L"Failed to instantate Opus encoder");

        int signal = OPUS_AUTO;
        switch (format.application) {
        case VoiceApplication::voip:
            signal = OPUS_SIGNAL_VOICE;
            break;

        case VoiceApplication::music:
            signal = OPUS_SIGNAL_MUSIC;
            break;
        }

        check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_SIGNAL_REQUEST, signal), L"Failed to set signal.");
        check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_PACKET_LOSS_PERC_REQUEST, 15), L"Failed to set packet loss percent.");
        check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_INBAND_FEC_REQUEST, 1), L"Failed to set fec.");
        check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_BITRATE_REQUEST, 131072), L"Failed to set bitrate.");
    }

    size_t OpusWrapper::Encode(array_view<uint8_t> pcm, gsl::span<uint8_t> target) {
        std::unique_lock lock(encode_mutex);

        try {
            auto duration = audio_format.CalculateSampleDuration(pcm.size());
            auto frame_size = audio_format.CalculateFrameSize(duration);
            auto sample_size = audio_format.CalculateSampleSize(duration);

            if (pcm.size() != sample_size)
                throw winrt::hresult_invalid_argument(L"Invalid PCM sample size.");

            int length = opus_encode(opus_encoder, (int16_t*)(pcm.data()), (int32_t)frame_size, target.data(), (int32_t)target.size());
            if (length < 0) {
                check_opus_error(length, L"Could not encode PCM to opus!");
            }

            return (size_t)length;
        }
        catch (winrt::hresult_invalid_argument&) {
            return 0;
        }
    }

    size_t OpusWrapper::EncodeFloat(array_view<uint8_t> pcm, gsl::span<uint8_t> target) {
        std::unique_lock lock(encode_mutex);

        try {
            auto duration = audio_format.CalculateSampleDurationF(pcm.size());
            auto frame_size = audio_format.CalculateFrameSize(duration);
            auto sample_size = audio_format.CalculateSampleSizeF(duration);

            if (pcm.size() != sample_size)
                throw winrt::hresult_invalid_argument(L"Invalid PCM sample size.");

            int length = opus_encode_float(opus_encoder, (float*)(pcm.data()), (int32_t)frame_size, target.data(), (int32_t)target.size());
            if (length < 0) {
                check_opus_error(length, L"Could not encode PCM to opus!");
            }

            return (size_t)length;
        }
        catch (winrt::hresult_invalid_argument&) {
            return 0;
        }
    }

    void OpusWrapper::Decode(AudioSource* decoder, array_view<uint8_t> opus, std::vector<uint8_t>& target, bool fec) {
        auto frames = opus_packet_get_nb_frames(opus.data(), opus.size());
        auto samples_per_frame = opus_packet_get_samples_per_frame(opus.data(), decoder->format.sample_rate);
        auto channels = opus_packet_get_nb_channels(opus.data());

        if (decoder->format.channel_count != (uint32_t)channels || !decoder->IsInitialised()) {
            decoder->format.channel_count = channels;
            decoder->Initialise(decoder->format);
        }

        auto sample_count = opus_decode(decoder->decoder, opus.data(), opus.size(), (int16_t*)target.data(), frames * samples_per_frame, fec);
        if (sample_count < 0) {
            check_opus_error(sample_count, L"Could not decode opus to PCM!");
        }

        auto sample_size = decoder->format.SampleCountToSampleSize(sample_count);
        target.resize(sample_size);
    }

    void OpusWrapper::ProcessPacketLoss(AudioSource* decoder, int32_t frame_size, std::vector<uint8_t>& target) {
        if (!decoder->IsInitialised()) {
            decoder->Initialise(decoder->format);
        }

        auto sample_count = opus_decode(decoder->decoder, nullptr, 0, (int16_t*)target.data(), frame_size, 1);
        if (sample_count < 0) {
            check_opus_error(sample_count, L"Could not decode opus to PCM!");
        }

        auto sample_size = decoder->format.SampleCountToSampleSize(sample_count);
        target.resize(sample_size);
    }

    AudioSource* OpusWrapper::GetOrCreateDecoder(uint32_t ssrc) {
        auto itr = opus_decoders.find(ssrc);
        if (itr == opus_decoders.end()) {
            auto source = new AudioSource(ssrc);
            opus_decoders.insert(std::pair(ssrc, source));
            return source;
        }
        else {
            return opus_decoders.at(ssrc);
        }
    }

    int32_t OpusWrapper::GetLastPacketSampleCount(OpusDecoder* decoder) {
        int32_t count;
        opus_decoder_ctl(decoder, OPUS_GET_LAST_PACKET_DURATION_REQUEST, &count);

        return count;
    }

    OpusWrapper::~OpusWrapper() {
        std::cout << "Freeing OpusWrapper\n";

        if (this->opus_encoder != nullptr) {
            opus_encoder_destroy(this->opus_encoder);
        }

        for (auto decoder : this->opus_decoders) {
            delete decoder.second;
        }

        this->opus_decoders.clear();
    }

    void OpusWrapper::check_opus_error(int error, winrt::hstring message) {
        switch (error) {
        case OPUS_BAD_ARG:
        case OPUS_BUFFER_TOO_SMALL:
        case OPUS_INVALID_PACKET:
            throw winrt::hresult_invalid_argument(message);
        case OPUS_INTERNAL_ERROR:
        case OPUS_INVALID_STATE:
            throw winrt::hresult_error(E_UNEXPECTED, message);
        case OPUS_UNIMPLEMENTED:
            throw winrt::hresult_not_implemented(message);
        case OPUS_ALLOC_FAIL:
            throw winrt::hresult_error(E_OUTOFMEMORY, message);
        default:
            return;
        }
    }
}
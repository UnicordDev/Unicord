#include "pch.h"
#include "OpusUtils.h"
#include "OpusDecoder.h"
#include "Rtp.h"

using namespace winrt::Unicord::Universal::Voice::Utilities;
using namespace winrt::Unicord::Universal::Voice::Transport;

namespace winrt::Unicord::Universal::Voice::Decode
{
    bool OpusDecoder::ProcessPacket(const RtpHeader& header, AudioSource** source, array_view<uint8_t>& decrypted_view, std::vector<std::vector<uint8_t>>& pcm)
    {
        AudioSource* audio_source = this->GetOrCreateDecoder(header.ssrc);
        *source = audio_source;

        if (!audio_source->is_speaking) {
            return false;
        }

        if (header.seq < audio_source->seq) { // out of order
            return false;
        }

        uint16_t gap = audio_source->seq != 0 ? header.seq - 1 - audio_source->seq : 0;
        if (gap != 0) {
            AudioFormat packet_format = audio_source->format;
            size_t fec_pcm_length = packet_format.SampleCountToSampleSize(packet_format.GetMaxBufferSize());

            if (gap == 1) {
                std::vector<uint8_t> fec(fec_pcm_length);
                this->Decode(audio_source, decrypted_view, fec, true);

                pcm.push_back(fec);
            }
            else if (gap > 1) {
                for (size_t i = 0; i < gap; i++) {
                    int32_t sample_count = this->GetLastPacketSampleCount(audio_source);
                    fec_pcm_length = packet_format.SampleCountToSampleSize(sample_count);
                    std::vector<uint8_t> fec(fec_pcm_length);
                    this->ProcessPacketLoss(audio_source, sample_count, fec);
                    pcm.push_back(fec);
                }
            }
        }

        size_t max_frame_size = audio_source->format.SampleCountToSampleSize(audio_source->format.GetMaxBufferSize());
        std::vector<uint8_t> raw_pcm(max_frame_size);

        this->Decode(audio_source, decrypted_view, raw_pcm, false);
        pcm.push_back(raw_pcm);

        audio_source->seq = header.seq;
        audio_source->packets_lost += gap;
        return true;
    }


    void OpusDecoder::Decode(AudioSource* decoder, array_view<uint8_t> opus, std::vector<uint8_t>& target, bool fec)
    {
        int32_t frames = opus_packet_get_nb_frames(opus.data(), opus.size());
        int32_t samples_per_frame = opus_packet_get_samples_per_frame(opus.data(), decoder->format.sample_rate);
        int32_t channels = opus_packet_get_nb_channels(opus.data());

        if (decoder->format.channel_count != (uint32_t)channels || !decoder->IsInitialised()) {
            decoder->format.channel_count = channels;
            decoder->Initialise(decoder->format);
        }

        auto sample_count = opus_decode(decoder->decoder, opus.data(), opus.size(), (int16_t*)target.data(), frames * samples_per_frame, fec);
        if (sample_count < 0) {
            OpusUtils::CheckOpusError(sample_count, L"Could not decode opus to PCM!");
        }

        auto sample_size = decoder->format.SampleCountToSampleSize(sample_count);
        target.resize(sample_size);
    }

    void OpusDecoder::ProcessPacketLoss(AudioSource* decoder, int32_t frame_size, std::vector<uint8_t>& target)
    {
        if (!decoder->IsInitialised()) {
            decoder->Initialise(decoder->format);
        }

        int32_t sample_count = opus_decode(decoder->decoder, nullptr, 0, (int16_t*)target.data(), frame_size, 1);
        if (sample_count < 0) {
             OpusUtils::CheckOpusError(sample_count, L"Could not decode opus to PCM!");
        }

        size_t sample_size = decoder->format.SampleCountToSampleSize(sample_count);
        target.resize(sample_size);
    }

    AudioSource* OpusDecoder::GetOrCreateDecoder(uint32_t ssrc)
    {
        auto itr = opusDecoders.find(ssrc);
        if (itr == opusDecoders.end()) {
            auto source = new AudioSource(ssrc);
            opusDecoders.insert(std::pair(ssrc, source));
            return source;
        }
        else {
            return opusDecoders.at(ssrc);
        }
    }

    AudioSource* OpusDecoder::GetAssociatedAudioSource(uint64_t user_id, bool remove)
    {
        for each (auto el in opusDecoders)
        {
            if (el.second->user_id == user_id) {
                if (remove) {
                    opusDecoders.erase(el.first);
                }

                return el.second;
            }
        }


        return nullptr;
    }

    int32_t OpusDecoder::GetLastPacketSampleCount(AudioSource* source)
    {
        int32_t count;
        opus_decoder_ctl(source->decoder, OPUS_GET_LAST_PACKET_DURATION_REQUEST, &count);

        return count;
    }

    OpusDecoder::~OpusDecoder()
    {
        for each (auto decoder in this->opusDecoders)
        {
            delete decoder.second;
        }

        this->opusDecoders.clear();
    }
}
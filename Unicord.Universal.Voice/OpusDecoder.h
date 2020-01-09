#pragma once
#include <opus.h>
#include "Rtp.h"
#include "AudioFormat.h"

using namespace winrt::Unicord::Universal::Voice::Interop;
using namespace winrt::Unicord::Universal::Voice::Transport;

namespace winrt::Unicord::Universal::Voice::Decode
{
    class OpusDecoder
    {
    public:
        bool ProcessPacket(const RtpPacket& header, AudioSource** source, array_view<uint8_t>& decrypted_view, std::vector<std::vector<uint8_t>>& pcm);
        void Decode(AudioSource* decoder, array_view<uint8_t> opus, std::vector<uint8_t>& target, bool fec);
        void ProcessPacketLoss(AudioSource* decoder, int32_t frame_size, std::vector<uint8_t>& target);

        AudioSource* GetOrCreateDecoder(uint32_t ssrc);
        AudioSource* GetAssociatedAudioSource(uint64_t user_id, bool remove = false);
        int32_t GetLastPacketSampleCount(AudioSource* decoder);

        ~OpusDecoder();
    private:
        std::map<uint32_t, AudioSource*> opusDecoders;
    };
}
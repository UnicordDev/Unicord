#include "pch.h"
#include <iostream>
#include "OpusWrapper.h"

namespace winrt::Unicord::Universal::Voice::Interop
{
    OpusWrapper::OpusWrapper(AudioFormat format)
    {
        int error;
        this->audio_format = format;
        this->opus_encoder = opus_encoder_create(format.sample_rate, format.channel_count, (int)format.application, &error);
        check_opus_error(error, L"Failed to instantate Opus encoder");

        int signal = OPUS_AUTO;
        switch (format.application)
        {
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

 

    OpusWrapper::~OpusWrapper()
    {
        std::cout << "Freeing OpusWrapper\n";
    }
}
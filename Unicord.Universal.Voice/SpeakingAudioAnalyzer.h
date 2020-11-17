#pragma once

#include <modules/audio_processing/include/audio_processing.h>

namespace winrt::Unicord::Universal::Voice::implementation {
    struct VoiceClient;
    class SpeakingAudioAnalyzer : public webrtc::CustomAudioAnalyzer {
    public:
        SpeakingAudioAnalyzer(VoiceClient* client) : _client(client) {}

        void Initialize(int32_t sample_rate_hz, int32_t num_channels) override;
        void Analyze(const webrtc::AudioBuffer* buffer) override;

        std::string ToString() const override {
            return "SpeakingAudioAnalyzer";
        }

    private:
        VoiceClient* _client;
        webrtc::AudioProcessing* _apm = nullptr;
    };
}
#include "pch.h"
#include "SpeakingAudioAnalyzer.h"
#include "VoiceClient.h"

namespace winrt::Unicord::Universal::Voice::implementation {
    void SpeakingAudioAnalyzer::Initialize(int32_t sample_rate_hz, int32_t num_channels) {
    }

    void SpeakingAudioAnalyzer::Analyze(const webrtc::AudioBuffer* buffer) {
        if (_apm == nullptr) {
            _apm = _client->_audioState->audio_processing();
        }

		if (!_client->_voiceOptions.VoiceActivity())
            return;

        if (_apm->voice_detection()->stream_has_voice() && !_client->is_muted) {
            _client->_outboundTransport->Start();
            _client->SendSpeakingAsync(true);
        }
        else {
            _client->_outboundTransport->Stop();
            _client->SendSpeakingAsync(false);
		}
    }
}
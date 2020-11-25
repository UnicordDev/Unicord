#pragma once
#include "VoiceClientOptions.g.h"

namespace winrt::Unicord::Universal::Voice::implementation {
    struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions> {
        VoiceClientOptions() = default;

        hstring Token();
        void Token(hstring value);
        hstring SessionId();
        void SessionId(hstring value);
        hstring Endpoint();
        void Endpoint(hstring value);
        uint64_t GuildId();
        void GuildId(uint64_t value);
        uint64_t ChannelId();
        void ChannelId(uint64_t value);
        uint64_t CurrentUserId();
        void CurrentUserId(uint64_t value);
        hstring PreferredPlaybackDevice();
        void PreferredPlaybackDevice(hstring value);
        hstring PreferredRecordingDevice();
        void PreferredRecordingDevice(hstring value);

        NoiseSuppressionLevel SuppressionLevel() {
            return noise_suppression_level;
        }

        void SuppressionLevel(NoiseSuppressionLevel value) {
            noise_suppression_level = value;
        }

        bool EchoCancellation() {
            return echo_cancellation;
        }

        void EchoCancellation(bool value) {
            echo_cancellation = value;
        }

        bool VoiceActivity() {
            return voice_activity;
        }

        void VoiceActivity(bool value) {
            voice_activity = value;
        }

        bool AutomaticGainControl() {
            return auto_gain_control;
        }

        void AutomaticGainControl(bool value) {
            auto_gain_control = value;
        }

    private:        
        hstring token = L"";
        hstring session_id = L"";
        hstring endpoint = L"";
        uint64_t guild_id = 0;
        uint64_t channel_id = 0;
        uint64_t current_user_id = 0;
        hstring preferred_playback_device = L"";
        hstring preferred_recording_device = L"";
        NoiseSuppressionLevel noise_suppression_level = NoiseSuppressionLevel::Medium;
        bool echo_cancellation = true;
        bool voice_activity = true;
        bool auto_gain_control = true;
    };
}
namespace winrt::Unicord::Universal::Voice::factory_implementation {
    struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions, implementation::VoiceClientOptions> {
    };
}

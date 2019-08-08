#pragma once
#include "VoiceClientOptions.g.h"

namespace winrt::Unicord::Universal::Voice::implementation
{
    struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions>
    {
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

	private:
		hstring token = L"";
		hstring session_id = L"";
		hstring endpoint = L"";
		uint64_t guild_id = 0;
		uint64_t channel_id = 0;
		uint64_t current_user_id = 0;
		hstring preferred_playback_device = L"";
		hstring preferred_recording_device = L"";		
    };
}
namespace winrt::Unicord::Universal::Voice::factory_implementation
{
    struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions, implementation::VoiceClientOptions>
    {
    };
}

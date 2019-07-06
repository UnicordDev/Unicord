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

	private:
		hstring token;
		hstring session_id;
		hstring endpoint;
		uint64_t guild_id;
		uint64_t channel_id;
		uint64_t current_user_id;
    };
}

namespace winrt::Unicord::Universal::Voice::factory_implementation
{
	struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions, implementation::VoiceClientOptions>
	{
	};
}

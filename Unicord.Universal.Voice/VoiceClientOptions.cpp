#include "pch.h"
#include "VoiceClientOptions.h"
#include "VoiceClientOptions.g.cpp"

namespace winrt::Unicord::Universal::Voice::implementation
{
    hstring VoiceClientOptions::Token()
    {
		return token;
    }

    void VoiceClientOptions::Token(hstring value)
    {
		token = value;
    }   

	hstring VoiceClientOptions::SessionId()
	{
		return session_id;
	}

	void VoiceClientOptions::SessionId(hstring value)
	{
		session_id = value;
	}
	
	hstring VoiceClientOptions::Endpoint()
	{
		return endpoint;
	}

	void VoiceClientOptions::Endpoint(hstring value)
	{
		endpoint = value;
	}

    uint64_t VoiceClientOptions::ChannelId()
    {
		return channel_id;
    }

    void VoiceClientOptions::ChannelId(uint64_t value)
    {
		channel_id = value;
    }

	uint64_t VoiceClientOptions::GuildId()
	{
		return guild_id;
	}

	void VoiceClientOptions::GuildId(uint64_t value)
	{
		guild_id = value;
	}

	uint64_t VoiceClientOptions::CurrentUserId()
	{
		return current_user_id;
	}

	void VoiceClientOptions::CurrentUserId(uint64_t value)
	{
		current_user_id = value;
	}
}

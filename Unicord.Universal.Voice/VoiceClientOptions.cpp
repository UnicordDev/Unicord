#include "pch.h"
#include "VoiceClientOptions.h"
#include "VoiceClientOptions.g.cpp"

namespace winrt::Unicord::Universal::Voice::implementation
{
	VoiceClientOptions::VoiceClientOptions() 
	{

	}

    hstring VoiceClientOptions::Token()
    {
		return token;
    }

    void VoiceClientOptions::Token(hstring value)
    {
		token = value;
    }

    uint64_t VoiceClientOptions::ChannelId()
    {
		return channel_id;
    }

    void VoiceClientOptions::ChannelId(uint64_t value)
    {
		channel_id = value;
    }
}

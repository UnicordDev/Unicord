#pragma once
#include "VoiceClientOptions.g.h"

namespace winrt::Unicord::Universal::Voice::implementation
{
    struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions>
    {
        VoiceClientOptions();

        hstring Token();
        void Token(hstring value);
        uint64_t ChannelId();
        void ChannelId(uint64_t value);
	private:
		hstring token;
		uint64_t channel_id;
    };
}

namespace winrt::Unicord::Universal::Voice::factory_implementation
{
	struct VoiceClientOptions : VoiceClientOptionsT<VoiceClientOptions, implementation::VoiceClientOptions>
	{
	};
}

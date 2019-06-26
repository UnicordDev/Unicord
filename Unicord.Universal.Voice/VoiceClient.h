#pragma once

#include "VoiceClient.g.h"

using namespace winrt::Windows::Networking::Sockets;

namespace winrt::Unicord::Universal::Voice::implementation
{
    struct VoiceClient : VoiceClientT<VoiceClient>
    {
		VoiceClient() = default;
		VoiceClient(Unicord::Universal::Voice::VoiceClientOptions const& options);

		static winrt::hstring OpusVersion();
		static winrt::hstring SodiumVersion();

		Windows::Foundation::IAsyncAction ConnectAsync();
		void Close();
	private:
		MessageWebSocket web_socket{ nullptr };
		DatagramSocket udp_socket{ nullptr };
		uint32_t ssrc;

		void LogMessage(std::string string);
		void OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev);
		void OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev);
    };
}

namespace winrt::Unicord::Universal::Voice::factory_implementation
{
    struct VoiceClient : VoiceClientT<VoiceClient, implementation::VoiceClient>
    {
    };
}

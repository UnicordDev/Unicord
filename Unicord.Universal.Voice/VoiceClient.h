#pragma once

#include "VoiceClient.g.h"
#include "SodiumWrapper.h"
#include "ConnectionEndpoint.h"
#include "AudioFormat.h"

#include <opus.h>
#include <sodium.h>
#include <string>
#include <iostream>
#include <chrono>
#include <sstream>
#include <debugapi.h>
#include <concurrent_unordered_map.h>

#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Storage.Streams.h>

using namespace winrt;
using namespace concurrency;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Data::Json;
using namespace winrt::Windows::System::Threading;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Networking::Sockets;
using namespace winrt::Unicord::Universal::Voice::Interop;

namespace winrt::Unicord::Universal::Voice::implementation
{
	struct VoiceClient : VoiceClientT<VoiceClient>
	{
		VoiceClient() = default;
		VoiceClient(VoiceClientOptions const& options);

		static hstring OpusVersion();
		static hstring SodiumVersion();

		uint32_t WebSocketPing();

		IAsyncAction ConnectAsync();
		void Close();
	private:
		VoiceClientOptions options{ nullptr };
		MessageWebSocket web_socket{ nullptr };
		DatagramSocket udp_socket{ nullptr };
		ThreadPoolTimer heartbeat_timer{ nullptr };
		ThreadPoolTimer keepalive_timer{ nullptr };
		SodiumWrapper sodium;

		std::pair<hstring, EncryptionMode> mode;
		ConnectionEndpoint endpoint;
		int32_t ssrc = 0;
		int32_t heartbeat_interval = 0;
		int32_t connection_stage = 0;

		volatile uint32_t ws_ping = 0;
		volatile uint32_t last_heartbeat = 0;

		volatile uint32_t udp_ping = 0;
		volatile uint64_t keepalive_count = 0;
		concurrent_unordered_map<uint64_t, uint64_t> keepalive_timestamps;

		IAsyncAction SendIdentifyAsync();
		IAsyncAction SendJsonPayloadAsync(JsonObject &payload);
		IAsyncAction Stage1(JsonObject obj);
		void Stage2(JsonObject obj);

		void HandleUdpHeartbeat(uint64_t reader);

		IAsyncAction OnWsHeartbeat(ThreadPoolTimer sender);
		IAsyncAction OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev);
		void OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev);

		IAsyncAction OnUdpHeartbeat(ThreadPoolTimer sender);
		IAsyncAction OnUdpMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs ev);
	};
}

class dbg_stream_for_cout: public std::stringbuf
{
public:
	~dbg_stream_for_cout() { sync(); }
	int sync()
	{
		::OutputDebugStringA(str().c_str());
		str(std::string()); // Clear the string buffer
		return 0;
	}
};

namespace winrt::Unicord::Universal::Voice::factory_implementation
{
	struct VoiceClient : VoiceClientT<VoiceClient, implementation::VoiceClient>
	{
	};
}

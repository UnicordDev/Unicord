#pragma once

#include "VoiceClient.g.h"
#include "SodiumWrapper.h"
#include "ConnectionEndpoint.h"
#include "AudioFormat.h"
#include "OpusWrapper.h"

#include <opus.h>
#include <sodium.h>
#include <string>
#include <iostream>
#include <chrono>
#include <sstream>
#include <debugapi.h>
#include <concurrent_unordered_map.h>
#include <concurrent_queue.h>

#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Storage.Streams.h>

using namespace winrt;
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
	public:
		VoiceClient() = default;
		VoiceClient(VoiceClientOptions const& options);

		AudioFormat audio_format;

		static hstring OpusVersion();
		static hstring SodiumVersion();

		uint32_t WebSocketPing();

		IAsyncAction ConnectAsync();
		IAsyncAction SendSpeakingAsync(bool speaking);
		Windows::Storage::Streams::IOutputStream GetOutputStream();
		void Close();

		array_view<const uint8_t> PreparePacket(array_view<uint8_t> pcm);
		void EnqueuePacket(VoicePacket packet);

		~VoiceClient();
	private:
		VoiceClientOptions options{ nullptr };
		MessageWebSocket web_socket{ nullptr };
		DatagramSocket udp_socket{ nullptr };
		ThreadPoolTimer heartbeat_timer{ nullptr };
		ThreadPoolTimer keepalive_timer{ nullptr };
		SodiumWrapper* sodium = nullptr;
		OpusWrapper* opus = nullptr;

		std::pair<hstring, EncryptionMode> mode;
		ConnectionEndpoint endpoint;

		bool is_speaking = false;

		uint16_t seq = 0;
		uint32_t ssrc = 0;
		uint32_t timestamp = 0;
		uint32_t nonce = 0;

		uint32_t heartbeat_interval = 0;
		uint32_t connection_stage = 0;

		volatile uint32_t ws_ping = 0;
		volatile uint32_t last_heartbeat = 0;

		volatile uint32_t udp_ping = 0;
		volatile uint64_t keepalive_count = 0;
		concurrency::concurrent_unordered_map<uint64_t, uint64_t> keepalive_timestamps;

		concurrency::concurrent_queue<VoicePacket> voice_queue;
		volatile bool cancel_voice_send = false;
		std::thread voice_thread;

		bool is_disposed = false;

		IAsyncAction SendIdentifyAsync();
		IAsyncAction SendJsonPayloadAsync(JsonObject &payload);
		IAsyncAction Stage1(JsonObject obj);
		void Stage2(JsonObject obj);

		void VoiceSendLoop();

		IAsyncAction OnWsHeartbeat(ThreadPoolTimer sender);
		IAsyncAction OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev);
		void OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev);

		IAsyncAction OnUdpHeartbeat(ThreadPoolTimer sender);
		void HandleUdpHeartbeat(uint64_t reader);
		IAsyncAction OnUdpMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs ev);

		friend struct VoiceOutputStream;
	};
}

class dbg_stream_for_cout : public std::stringbuf
{
public:
	virtual int_type overflow(int_type c = EOF) {
		if (c != EOF) {
			TCHAR buf[] = { c, '\0' };
			OutputDebugString(buf);
		}
		return c;
	}
};

namespace winrt::Unicord::Universal::Voice::factory_implementation
{
	struct VoiceClient : VoiceClientT<VoiceClient, implementation::VoiceClient>
	{
	};
}

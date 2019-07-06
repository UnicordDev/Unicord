#include "pch.h"
#include "VoiceClient.h"
#include "VoiceClient.g.cpp"

using namespace winrt;
using namespace std::chrono;
using namespace winrt::Windows::Data::Json;
using namespace winrt::Windows::Networking;
using namespace winrt::Windows::Networking::Sockets;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Unicord::Universal::Voice::Interop;

namespace winrt::Unicord::Universal::Voice::implementation
{
	hstring VoiceClient::OpusVersion()
	{
		auto strptr = opus_get_version_string();
		return to_hstring(strptr);
	}

	hstring VoiceClient::SodiumVersion()
	{
		auto strptr = sodium_version_string();
		return to_hstring(strptr);
	}

	VoiceClient::VoiceClient(VoiceClientOptions const& options)
	{
		sodium_init();

		auto stream = new dbg_stream_for_cout();
		std::cout.rdbuf(stream); 
		std::cout << std::unitbuf;

		if (options.Token().size() == 0 && options.ChannelId() == 0) {
			throw hresult_invalid_argument();
		}

		this->options = options;
		web_socket = MessageWebSocket();
		udp_socket = DatagramSocket();

		std::wstring_view raw_endpoint = options.Endpoint();
		std::size_t index = raw_endpoint.find_last_of(':');

		if (index != std::wstring::npos) {
			std::wstring str(raw_endpoint.substr(index + 1));
			endpoint.hostname = raw_endpoint.substr(0, index);
			endpoint.port = (uint16_t)std::stoi(str);
		}
		else {
			endpoint.hostname = raw_endpoint;
			endpoint.port = 80;
		}

		udp_socket.Control().QualityOfService(SocketQualityOfService::LowLatency);
		udp_socket.MessageReceived({ this, &VoiceClient::OnUdpMessage });

		web_socket.Control().MessageType(SocketMessageType::Utf8);
		web_socket.MessageReceived({ this, &VoiceClient::OnWsMessage });
		web_socket.Closed({ this, &VoiceClient::OnWsClosed });
	}

	uint32_t VoiceClient::WebSocketPing()
	{
		return ws_ping;
	}

	IAsyncAction VoiceClient::ConnectAsync()
	{
		Windows::Foundation::Uri url{ L"wss://" + endpoint.hostname + L"/?encoding=json&v=4" };
		co_await web_socket.ConnectAsync(url);
	}

	IAsyncAction VoiceClient::SendIdentifyAsync()
	{
		heartbeat_timer = ThreadPoolTimer::CreatePeriodicTimer({ this, &VoiceClient::OnWsHeartbeat }, milliseconds(heartbeat_interval));

		auto payload = JsonObject();
		payload.SetNamedValue(L"server_id", JsonValue::CreateStringValue(to_hstring(options.GuildId())));
		payload.SetNamedValue(L"user_id", JsonValue::CreateStringValue(to_hstring(options.CurrentUserId())));
		payload.SetNamedValue(L"session_id", JsonValue::CreateStringValue(options.SessionId()));
		payload.SetNamedValue(L"token", JsonValue::CreateStringValue(options.Token()));

		auto disp = JsonObject();
		disp.SetNamedValue(L"op", JsonValue::CreateNumberValue(0));
		disp.SetNamedValue(L"d", payload);

		co_await SendJsonPayloadAsync(disp);
	}

	IAsyncAction VoiceClient::Stage1(JsonObject obj)
	{
		HostName remoteHost{ endpoint.hostname };
		EndpointPair pair{ nullptr, L"", remoteHost, to_hstring(endpoint.port) };
		co_await udp_socket.ConnectAsync(pair);

		mode = SodiumWrapper::SelectEncryptionMode(obj.GetNamedArray(L"modes"));

		uint8_t buff[70];
		memcpy_s(&buff, 70, &ssrc, sizeof(uint16_t));

		DataWriter writer{ udp_socket.OutputStream() };
		writer.WriteBytes(buff);
		co_await writer.StoreAsync();
		writer.DetachStream();
	}

	void VoiceClient::Stage2(JsonObject data)
	{
		auto secret_key = data.GetNamedArray(L"secret_key");
		auto mode = data.GetNamedString(L"mode");

		uint8_t key[32];
		for (size_t i = 0; i < 32; i++)
		{
			key[i] = (uint8_t)secret_key.GetNumberAt(i);
		}

		sodium = SodiumWrapper(array_view(&key[0], &key[secret_key.Size()]), SodiumWrapper::GetEncryptionMode(mode));
		keepalive_timer = ThreadPoolTimer::CreatePeriodicTimer({ this, &VoiceClient::OnUdpHeartbeat }, milliseconds(5000));
	}


	IAsyncAction VoiceClient::OnWsHeartbeat(ThreadPoolTimer timer)
	{
		try
		{
			auto stamp = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
			last_heartbeat = stamp;

			JsonObject disp;
			disp.SetNamedValue(L"op", JsonValue::CreateNumberValue(3));
			disp.SetNamedValue(L"d", JsonValue::CreateStringValue(to_hstring(stamp)));

			co_await SendJsonPayloadAsync(disp);
		}
		catch (const std::exception& ex)
		{
			std::cout << "ERROR: " << ex.what() << "\n";
		}
	}

	IAsyncAction VoiceClient::OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev)
	{
		auto reader = ev.GetDataReader();
		reader.UnicodeEncoding(UnicodeEncoding::Utf8);
		auto json_data = reader.ReadString(reader.UnconsumedBufferLength());
		reader.Close();

		std::cout << "↓ " << to_string(json_data) << "\n";

		auto json = JsonObject::Parse(json_data);
		auto op = (int)json.GetNamedNumber(L"op");
		auto value = json.GetNamedValue(L"d");

		if (value.ValueType() == JsonValueType::Object) {
			auto data = value.GetObject();
			switch (op) {
			case 8: // hello
				heartbeat_interval = (int32_t)data.GetNamedNumber(L"heartbeat_interval");
				co_await SendIdentifyAsync();
				break;
			case 2: // ready
				ssrc = (int32_t)data.GetNamedNumber(L"ssrc");
				endpoint.port = (uint16_t)data.GetNamedNumber(L"port");
				co_await Stage1(data);
				break;
			case 4: // session description
				Stage2(data);
				break;
			}
		}

		if (value.ValueType() == JsonValueType::String) {
			auto data = value.GetString();
			switch (op) {
			case 6: // heartbeat ack
				auto now = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
				ws_ping = now - last_heartbeat;

				std::cout << "- WS Ping " << ws_ping << "ms\n";
				break;
			}
		}
	}

	void VoiceClient::OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev)
	{
		try
		{
			std::cout << "WebSocket closed with code " << ev.Code() << " and reason " << to_string(ev.Reason()) << "\n";
			Close();
		}
		catch (const std::exception&)
		{

		}
	}

	IAsyncAction VoiceClient::OnUdpHeartbeat(ThreadPoolTimer timer)
	{
		try
		{
			DataWriter writer{ udp_socket.OutputStream() };

			uint64_t now = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
			uint64_t count = keepalive_count;
			keepalive_count = count + 1;
			keepalive_timestamps.insert({ count, now });

			std::cout << "↑ Sending UDP Heartbeat " << count << "\n";

			writer.WriteUInt64(count);
			co_await writer.StoreAsync();
			writer.DetachStream();
		}
		catch (const std::exception& ex)
		{
			std::cout << "ERROR: " << ex.what() << "\n";
			Close();
		}
	}

	void VoiceClient::HandleUdpHeartbeat(uint64_t count)
	{
		uint64_t now = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
		std::cout << "↓ Got UDP Heartbeat " << count << "!\n";

		auto itr = keepalive_timestamps.find(count);
		if (itr != keepalive_timestamps.end()) {
			uint64_t then = keepalive_timestamps.at(count);
			udp_ping = now - then;
			keepalive_timestamps.unsafe_erase(count);

			std::cout << "- UDP Ping " << udp_ping << "ms\n";
		}
	}

	IAsyncAction VoiceClient::OnUdpMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs ev)
	{
		auto reader = ev.GetDataReader();

		if (connection_stage == 0) {
			uint8_t buff[70];
			reader.ReadBytes(buff);

			std::string ip{ &buff[4], &buff[64] };
			ip = ip.substr(0, ip.find_first_of('\0'));

			uint16_t port = *(uint16_t*)&buff[68];

			JsonObject data;
			data.SetNamedValue(L"address", JsonValue::CreateStringValue(to_hstring(ip)));
			data.SetNamedValue(L"port", JsonValue::CreateNumberValue(port));
			data.SetNamedValue(L"mode", JsonValue::CreateStringValue(mode.first));

			JsonObject protocol_select;
			protocol_select.SetNamedValue(L"protocol", JsonValue::CreateStringValue(L"udp"));
			protocol_select.SetNamedValue(L"data", data);

			JsonObject dispatch;
			dispatch.SetNamedValue(L"op", JsonValue::CreateNumberValue(1));
			dispatch.SetNamedValue(L"d", protocol_select);

			co_await SendJsonPayloadAsync(dispatch);

			connection_stage = 1;
		}
		else {
			auto len = reader.UnconsumedBufferLength();
			if (len == 8) {
				uint64_t count = reader.ReadUInt64();

				HandleUdpHeartbeat(count);
			}
			else {
				// prolly voice data
			}
		}
	}

	IAsyncAction VoiceClient::SendJsonPayloadAsync(JsonObject &payload)
	{
		DataWriter writer{ web_socket.OutputStream() };
		auto str = payload.Stringify();

		std::cout << "↑ " << to_string(str) << "\n";

		writer.WriteString(str);
		co_await writer.StoreAsync();
		writer.DetachStream();
	}

	void VoiceClient::Close()
	{
		heartbeat_timer.Cancel();
		keepalive_timer.Cancel();

		web_socket.Close();
		udp_socket.Close();
	}
}

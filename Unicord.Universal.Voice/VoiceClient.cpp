#include "pch.h"
#include "Rtp.h"
#include "VoiceOutputStream.h"
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
		if (sodium_init() == -1) {
			throw hresult_error(E_UNEXPECTED, L"Failed to initialize libsodium!");
		}

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

		audio_format = AudioFormat();
		opus = new OpusWrapper(audio_format);

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

		sodium = new SodiumWrapper(array_view(&key[0], &key[secret_key.Size()]), SodiumWrapper::GetEncryptionMode(mode));
		keepalive_timer = ThreadPoolTimer::CreatePeriodicTimer({ this, &VoiceClient::OnUdpHeartbeat }, milliseconds(5000));
		voice_thread = std::thread(&VoiceClient::VoiceSendLoop, this);

		auto size = audio_format.CalculateSampleSize(20);
		auto null_pcm = new uint8_t[size]{ 0 };

		for (size_t i = 0; i < 3; i++)
		{
			auto null_packet = PreparePacket(array_view<uint8_t>(&null_pcm[0], &null_pcm[size]));
			voice_queue.push(VoicePacket(null_packet, 20, false));
		}

		delete[] null_pcm;
	}

	void VoiceClient::VoiceSendLoop()
	{
		VoicePacket packet;
		DataWriter writer{ udp_socket.OutputStream() };

		auto start_time = high_resolution_clock::now();
		while (!cancel_voice_send) {
			bool has_packet = voice_queue.try_pop(packet);

			array_view<const uint8_t> packet_array;
			if (has_packet) {
				packet_array = packet.bytes;
			}

			// Provided by Laura#0090 (214796473689178133); this is Python, but adaptable:
			// 
			// delay = max(0, self.delay + ((start_time + self.delay * loops) + - time.time()))
			// 
			// self.delay
			//   sample size
			// start_time
			//   time since streaming started
			// loops
			//   number of samples sent
			// time.time()
			//   DateTime.Now

			duration packet_duration = has_packet ? milliseconds(packet.duration) : 20ms;
			duration current_time_offset = high_resolution_clock::now() - start_time;

			if (current_time_offset < packet_duration) {
				std::this_thread::sleep_for(packet_duration - current_time_offset);
			}

			start_time += packet_duration;

			if (!has_packet)
				continue;

			SendSpeakingAsync(true).get();

			writer.WriteBytes(packet_array);
			writer.StoreAsync().get();

			if (!packet.is_silence && voice_queue.unsafe_size() == 0) {
				auto size = audio_format.CalculateSampleSize(20);
				auto null_pcm = new uint8_t[size]{ 0 };

				for (size_t i = 0; i < 3; i++)
				{
					auto null_packet = PreparePacket(array_view<uint8_t>(&null_pcm[0], &null_pcm[size]));
					voice_queue.push(VoicePacket(null_packet, 20, true));
				}

				delete[] null_pcm;
			}

			else if (voice_queue.unsafe_size() == 0) {
				SendSpeakingAsync(false).get();
			}

			// delete[] packet.bytes.begin();
		}
	}

	array_view<const uint8_t> VoiceClient::PreparePacket(array_view<uint8_t> pcm)
	{
		auto audio_size = audio_format.SampleCountToSampleSize(audio_format.GetMaxBufferSize());
		auto packet_array_size = Rtp::CalculatePacketSize(audio_size, mode.second);
		auto packet_array = new uint8_t[packet_array_size]{ 0 };

		Rtp::EncodeHeader(seq, timestamp, ssrc, packet_array, packet_array_size);

		auto opus_length = opus->Encode(pcm, array_view(packet_array + Rtp::HEADER_SIZE, packet_array + packet_array_size));
		array_view<uint8_t> opus_data(&packet_array[Rtp::HEADER_SIZE], &packet_array[Rtp::HEADER_SIZE + opus_length]);

		auto time = audio_format.CalculateFrameSize(audio_format.CalculateSampleDuration(pcm.size()));
		this->seq++;
		this->timestamp += time;

		uint8_t nonce[crypto_secretbox_xsalsa20poly1305_NONCEBYTES] = { 0 };
		switch (mode.second)
		{
		case EncryptionMode::XSalsa20_Poly1305:
			sodium->GenerateNonce(array_view<uint8_t>(packet_array, packet_array + Rtp::HEADER_SIZE), nonce, crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
			break;
		case EncryptionMode::XSalsa20_Poly1305_Suffix:
			sodium->GenerateNonce(nonce, crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
			break;
		case EncryptionMode::XSalsa20_Poly1305_Lite:
			sodium->GenerateNonce(this->nonce++, nonce, crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
			break;
		}

		auto encrypted_size = sodium->CalculateTargetSize(opus_data.size());
		auto encrypted = new uint8_t[encrypted_size];

		sodium->Encrypt(opus_data, array_view<uint8_t>(nonce), encrypted, encrypted_size);
		std::copy(&encrypted[0], &encrypted[encrypted_size], &packet_array[Rtp::HEADER_SIZE]);

		auto new_packet_size = Rtp::CalculatePacketSize(encrypted_size, mode.second);
		auto new_packet = new uint8_t[new_packet_size];
		std::copy(&packet_array[0], &packet_array[new_packet_size], new_packet);
		sodium->AppendNonce(array_view<uint8_t>(nonce), new_packet, new_packet_size, mode.second);

		delete[] encrypted;
		delete[] packet_array;

		return array_view<const uint8_t>(new_packet, new_packet + new_packet_size);
	}

	void VoiceClient::EnqueuePacket(VoicePacket packet)
	{
		voice_queue.push(packet);
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

	IAsyncAction VoiceClient::SendSpeakingAsync(bool speaking)
	{
		if (is_speaking == speaking)
			return;

		is_speaking = speaking;

		JsonObject payload;
		payload.SetNamedValue(L"speaking", JsonValue::CreateBooleanValue(speaking));
		payload.SetNamedValue(L"delay", JsonValue::CreateNumberValue(0));

		JsonObject disp;
		disp.SetNamedValue(L"op", JsonValue::CreateNumberValue(5));
		disp.SetNamedValue(L"d", payload);

		co_await SendJsonPayloadAsync(disp);
	}

	Windows::Storage::Streams::IOutputStream VoiceClient::GetOutputStream()
	{
		return make_self<VoiceOutputStream>(*this).as<IOutputStream>();
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
				HandleUdpHeartbeat(reader.ReadUInt64());
			}
			else {
				// prolly voice data
				// TODO: decode packet
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
		if (is_disposed)
			return;

		is_disposed = true;

		if (opus != nullptr)
			delete opus;

		if (sodium != nullptr)
			delete sodium;

		heartbeat_timer.Cancel();
		keepalive_timer.Cancel();

		web_socket.Close();
		udp_socket.Close();
	}

	VoiceClient::~VoiceClient()
	{
		Close();
	}
}

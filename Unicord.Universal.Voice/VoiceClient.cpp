#include "pch.h"
#include "Rtp.h"
#include "VoiceOutputStream.h"
#include "VoiceClient.h"
#include "VoiceClient.g.cpp"
#include <iomanip>
#include <bitset>
#include <VideoEventArgs.h>
using namespace winrt::Unicord::Universal::Voice::Transport;

using namespace winrt;
using namespace std::chrono;
using namespace winrt::Windows::Data::Json;
using namespace winrt::Windows::Networking;
using namespace winrt::Windows::Networking::Sockets;
using namespace winrt::Windows::Storage;
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
        auto stream = new dbg_stream_for_cout();
        std::cout.rdbuf(stream);
        std::cout << std::unitbuf;

        if (sodium_init() == -1) {
            throw hresult_error(E_UNEXPECTED, L"Failed to initialize libsodium!");
        }

        if (options.Token().size() == 0 && options.ChannelId() == 0) {
            throw hresult_invalid_argument();
        }

        this->options = options;
        this->opusDecoder = new Decode::OpusDecoder();
        this->h264Decoder = new Decode::H264Decoder();

        std::wstring_view raw_endpoint = options.Endpoint();
        std::size_t index = raw_endpoint.find_last_of(':');

        if (index != std::wstring::npos) {
            std::wstring str(raw_endpoint.substr(index + 1));
            wsEndpoint.hostname = raw_endpoint.substr(0, index);
            wsEndpoint.port = (uint16_t)std::stoi(str);
        }
        else {
            wsEndpoint.hostname = raw_endpoint;
            wsEndpoint.port = 443;
        }

        InitialiseSockets();
    }

    void VoiceClient::InitialiseSockets()
    {
        audio_format = AudioFormat();
        udpSocket = DatagramSocket();
        udpSocket.Control().QualityOfService(SocketQualityOfService::LowLatency);
        udpSocket.MessageReceived({ this, &VoiceClient::OnUdpMessage });

        webSocket = MessageWebSocket();
        webSocket.Control().MessageType(SocketMessageType::Utf8);
        webSocket.MessageReceived({ this, &VoiceClient::OnWsMessage });
        webSocket.Closed({ this, &VoiceClient::OnWsClosed });
    }

    IAsyncAction VoiceClient::ConnectAsync()
    {
        renderer = new AudioRenderer(this);
        renderer->Initialise(options.PreferredPlaybackDevice(), options.PreferredRecordingDevice());

        auto format = renderer->GetRenderProperties();
        std::cout << "Render: " << format.SampleRate() << " " << format.ChannelCount() << " " << format.BitsPerSample() << "\n";
        format = renderer->GetCaptureProperties();
        std::cout << "Capture: " << format.SampleRate() << " " << format.ChannelCount() << " " << format.BitsPerSample() << "\n";

        audio_format = AudioFormat(format.SampleRate(), format.ChannelCount(), VoiceApplication::low_latency);
        opusEncoder = new Encode::OpusEncoder(audio_format);

        Windows::Foundation::Uri url{ L"wss://" + wsEndpoint.hostname + L"/?encoding=json&v=4" };
        co_await webSocket.ConnectAsync(url);
    }

    bool VoiceClient::Muted()
    {
        return is_muted;
    }

    void VoiceClient::Muted(bool value)
    {
        if (is_muted != value && renderer != nullptr) {
            if (value) {
                renderer->StopCapture();
            }
            else {
                renderer->BeginCapture();
            }
        }

        is_muted = value;
    }

    bool VoiceClient::Deafened()
    {
        return is_deafened;
    }

    void VoiceClient::Deafened(bool value)
    {
        if (is_muted != value && renderer != nullptr) {
            if (value) {
                renderer->StopCapture();
            }
            else {
                renderer->BeginCapture();
            }
        }

        if (is_deafened != value && renderer != nullptr) {
            if (value) {
                renderer->StopRender();
            }
            else {
                renderer->BeginRender();
            }
        }

        is_muted = value;
        is_deafened = value;
    }

    IAsyncAction VoiceClient::SendIdentifyAsync(bool isResume)
    {
        heartbeatTimer = ThreadPoolTimer::CreatePeriodicTimer({ this, &VoiceClient::OnWsHeartbeat }, milliseconds(heartbeatInterval));

        JsonObject payload;
        if (options.GuildId() != 0) {
            payload.SetNamedValue(L"server_id", JsonValue::CreateStringValue(to_hstring(options.GuildId())));
        }
        else {
            payload.SetNamedValue(L"server_id", JsonValue::CreateStringValue(to_hstring(options.ChannelId())));
        }

        if (!isResume)
            payload.SetNamedValue(L"user_id", JsonValue::CreateStringValue(to_hstring(options.CurrentUserId())));

        payload.SetNamedValue(L"session_id", JsonValue::CreateStringValue(options.SessionId()));
        payload.SetNamedValue(L"token", JsonValue::CreateStringValue(options.Token()));
        payload.SetNamedValue(L"video", JsonValue::CreateBooleanValue(true));

        JsonObject disp;
        disp.SetNamedValue(L"op", JsonValue::CreateNumberValue(isResume ? 7 : 0));
        disp.SetNamedValue(L"d", payload);

        co_await SendJsonPayloadAsync(disp);
    }

    IAsyncAction VoiceClient::Stage1(JsonObject obj)
    {
        if (obj != nullptr) {
            udpEndpoint.hostname = obj.GetNamedString(L"ip");
            udpEndpoint.port = (uint16_t)obj.GetNamedNumber(L"port");
        }

        HostName remoteHost{ udpEndpoint.hostname };
        EndpointPair pair{ nullptr, L"", remoteHost, to_hstring(udpEndpoint.port) };
        co_await udpSocket.ConnectAsync(pair);

        mode = SodiumWrapper::SelectEncryptionMode(obj.GetNamedArray(L"modes"));

        uint8_t buff[70]{ 0 };
        std::copy(&audioSSRC, &audioSSRC + sizeof audioSSRC, buff);

        DataWriter writer{ udpSocket.OutputStream() };
        writer.WriteBytes(buff);
        co_await writer.StoreAsync();
        writer.DetachStream();
    }

    void VoiceClient::Stage2(JsonObject data)
    {
        if (data != nullptr) {
            JsonArray secret_key = data.GetNamedArray(L"secret_key");
            hstring new_mode = data.GetNamedString(L"mode");

            uint8_t key[32];
            for (uint32_t i = 0; i < 32; i++)
            {
                key[i] = (uint8_t)secret_key.GetNumberAt(i);
            }

            sodium = new SodiumWrapper(array_view<const uint8_t>(key), SodiumWrapper::GetEncryptionMode(new_mode));
        }

        keepaliveTimer = ThreadPoolTimer::CreatePeriodicTimer({ this, &VoiceClient::OnUdpHeartbeat }, 5000ms);
        voice_thread = std::thread(&VoiceClient::VoiceSendLoop, this);

        auto size = audio_format.CalculateSampleSize(20);
        for (size_t i = 0; i < 3; i++)
        {
            uint8_t* null_pcm = new uint8_t[size]{ 0 };
            voice_queue.push(PCMPacket(gsl::make_span(null_pcm, size), 20));
        }

        if (!is_deafened)
            renderer->BeginRender();

        if (!is_muted && !is_deafened)
            renderer->BeginCapture();

        connected(*this, true);
    }

    void VoiceClient::VoiceSendLoop()
    {
        try
        {
            PCMPacket packet;
            DataWriter writer{ udpSocket.OutputStream() };

            auto start_time = high_resolution_clock::now();
            while (!cancel_voice_send) {
                bool has_packet = voice_queue.try_pop(packet) && packet.duration != 0;

                gsl::span<uint8_t> packet_array;
                if (has_packet) {
                    packet_array = packet.bytes;
                }

                // Provided by Laura#0090 (214796473689178133); this is Python, but adaptable:
                // 
                // delay = max(0, self.delay + ((start_time + self.delay * loops) + - time.time()))

                duration packet_duration = has_packet ? milliseconds(packet.duration) : 20ms;
                duration current_time_offset = high_resolution_clock::now() - start_time;

                if (current_time_offset < packet_duration) {
                    std::this_thread::sleep_for(packet_duration - current_time_offset);
                }

                start_time += packet_duration;

                if (!has_packet || is_deafened)
                {
                    SendSpeakingAsync(false).get();
                    continue;
                }

                SendSpeakingAsync(true).get();

                auto opus_packet = PreparePacket(array_view(packet_array.data(), packet_array.data() + packet_array.size()), packet.is_silence, packet.is_float);
                writer.WriteBytes(array_view<const uint8_t>(opus_packet.bytes.data(), opus_packet.bytes.data() + opus_packet.bytes.size()));
                writer.StoreAsync().get();

                delete[] packet_array.data();

                if (!packet.is_silence && voice_queue.unsafe_size() == 0) {
                    auto size = audio_format.CalculateSampleSize(20);
                    for (size_t i = 0; i < 3; i++)
                    {
                        auto null_pcm = new uint8_t[size]{ 0 };
                        voice_queue.push(PCMPacket(gsl::make_span(null_pcm, size), 20));
                    }
                }
                else if (voice_queue.unsafe_size() == 0) {
                    SendSpeakingAsync(false).get();
                }
            }
        }
        catch (const winrt::hresult_error & ex)
        {
            if (!ws_closed)
                webSocket.Close();
            std::cout << "ERROR: " << to_string(ex.message()) << "\n";
        }
    }

    VoicePacket VoiceClient::PreparePacket(array_view<uint8_t> pcm, bool silence, bool is_float)
    {
        if (is_disposed)
            return VoicePacket();

        if (pcm.size() == 0)
            return VoicePacket();

        RtpHeader header{ Rtp::RTP_TYPE_OPUS,audioSequence, audioTimestamp, audioSSRC };
        auto packet_size = audio_format.GetMaxBufferSize() * 2;
        auto packet = std::vector<uint8_t>(packet_size);
        auto packet_span = gsl::make_span(packet);

        auto opus_length = is_float ? opusEncoder->EncodeFloat(pcm, packet_span) : opusEncoder->Encode(pcm, packet_span);
        auto encrypted_size = sodium->CalculateTargetSize(opus_length);
        auto new_packet_size = Rtp::CalculatePacketSize((uint32_t)encrypted_size, header, mode.second);
        auto new_packet = std::vector<uint8_t>(new_packet_size);
        auto new_packet_span = gsl::make_span(new_packet);

        Rtp::EncodeHeader(header, new_packet_span);

        const size_t size = crypto_secretbox_xsalsa20poly1305_NONCEBYTES;
        uint8_t packet_nonce[size] = { 0 };
        switch (mode.second)
        {
        case EncryptionMode::XSalsa20_Poly1305:
            sodium->GenerateNonce(new_packet_span.subspan(0, header.size()), packet_nonce);
            break;
        case EncryptionMode::XSalsa20_Poly1305_Suffix:
            sodium->GenerateNonce(packet_nonce);
            break;
        case EncryptionMode::XSalsa20_Poly1305_Lite:
            sodium->GenerateNonce(this->nonce++, packet_nonce);
            break;
        }

        sodium->Encrypt(packet_span.subspan(0, opus_length), packet_nonce, new_packet_span.subspan(header.size(), encrypted_size));
        sodium->AppendNonce(packet_nonce, new_packet_span, mode.second);

        auto duration = is_float ? audio_format.CalculateSampleDurationF(pcm.size()) : audio_format.CalculateSampleDuration(pcm.size());
        auto time = audio_format.CalculateFrameSize(duration);
        this->audioSequence++;
        this->audioTimestamp += (uint32_t)time;

        return VoicePacket{ new_packet, duration, silence };
    }

    void VoiceClient::EnqueuePacket(PCMPacket packet)
    {
        if (packet.duration != 0)
            voice_queue.push(packet);
    }

    void VoiceClient::ProcessRawPacket(array_view<uint8_t> data)
    {
        AudioSource* source;
        std::vector<std::vector<uint8_t>> pcm_packets;

        if (ProcessIncomingPacket(array_view<const uint8_t>(data.begin(), data.end()), pcm_packets, &source)) {
            for each (auto raw_packet in pcm_packets)
            {
                renderer->ProcessIncomingPacket(raw_packet, source);
            }
        }

        pcm_packets.clear();
    }

    bool VoiceClient::ProcessIncomingPacket(array_view<const uint8_t> data, std::vector<std::vector<uint8_t>>& pcm, AudioSource** source)
    {
        bool ret = false;

        if (!Rtp::IsRtpHeader(data))
            return false;

        if (is_disposed)
            return false;

        // decode RTP header
        RtpHeader header;
        Rtp::DecodeHeader(data, header);

        if (header.type == 73 || header.type == 72) {
            return false;
        }

        // get the nonce
        uint8_t packet_nonce[crypto_secretbox_xsalsa20poly1305_NONCEBYTES]{ 0 };
        array_view<uint8_t> nonce_view(packet_nonce);
        sodium->GetNonce(data, nonce_view, header, mode.second);

        // get the data
        array_view<const uint8_t> encrypted_data;
        Rtp::GetDataFromPacket(data, encrypted_data, header, mode.second);

        // calculate the size of the decrypted data
        size_t decrypted_size = sodium->CalculateSourceSize(encrypted_data.size());
        uint8_t* decrypted_data = new uint8_t[decrypted_size]{ 0 };
        array_view<uint8_t> decrypted_view(decrypted_data, decrypted_data + decrypted_size);

        // decrypt the recieved data
        try
        {
            sodium->Decrypt(encrypted_data, nonce_view, decrypted_view);

            // strip extensions
            if (header.extension) {
                // RFC 5285, 4.2 One-Byte header
                // http://www.rfcreader.com/#rfc5285_line186
                if (decrypted_view[0] == 0xBE && decrypted_view[1] == 0xDE) {
                    uint16_t headerLen = decrypted_view[2] << 8 | decrypted_view[3];
                    uint8_t i = 4;
                    for (; i < headerLen + 4; i++)
                    {
                        uint8_t b = decrypted_view[i];

                        // ID is currently unused since we skip it anyway
                        uint8_t id = (uint8_t)(b >> 4);
                        uint8_t length = (uint8_t)(b & 0x0F) + 1;
                        i += length;
                    }

                    // Strip extension padding too
                    while (decrypted_view[i] == 0)
                        i++;

                    decrypted_view = array_view(decrypted_view.begin() + i, decrypted_view.end());
                }
            }

            if (decrypted_view[0] == 0x90) {
                decrypted_view = array_view(decrypted_view.begin() + 2, decrypted_view.end());
            }
            if (header.type == Rtp::RTP_TYPE_OPUS) { // opus data

                if (is_deafened) {
                    ret = false;
                }
                else {
                    ret = opusDecoder->ProcessPacket(header, source, decrypted_view, pcm);
                }
            }

            if (header.type == Rtp::RTP_TYPE_H264) { // video 
                std::ostringstream str;
                str << std::setfill('0') << std::setw(4) << std::hex;
                for (size_t i = 0; i < decrypted_view.size(); i++)
                {
                    str << (uint32_t)decrypted_view[i] << " ";
                }
                std::cout << str.str() << std::endl;

                H264Frame frame;
                if (h264Decoder->ProcessPacket(header, decrypted_view, frame)) {
                    std::cout << "packets:" << frame.data.size() << " pps:" << frame.pps.size() << " sps:" << frame.sps.size() << std::endl;
                }
            }
        }
        catch (const winrt::hresult_error & ex)
        {
            std::cout << (uint32_t)header.type << " " << header.ssrc << " " << data.size() << "\n";
        }

        delete[] decrypted_data;

        return ret;
    }

   
    IAsyncAction VoiceClient::OnWsHeartbeat(ThreadPoolTimer timer)
    {
        uint32_t stamp = (uint32_t)duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
        last_heartbeat = stamp;

        JsonObject disp;
        disp.SetNamedValue(L"op", JsonValue::CreateNumberValue(3));
        disp.SetNamedValue(L"d", JsonValue::CreateStringValue(to_hstring(stamp)));

        co_await SendJsonPayloadAsync(disp);
    }

    IAsyncAction VoiceClient::OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev)
    {
        try
        {
            ws_closed = false;
            auto reader = ev.GetDataReader();
            reader.UnicodeEncoding(UnicodeEncoding::Utf8);
            auto json_data = reader.ReadString(reader.UnconsumedBufferLength());
            reader.Close();

            std::cout << "> " << to_string(json_data) << "\n";

            auto json = JsonObject::Parse(json_data);
            auto op = (int)json.GetNamedNumber(L"op");
            auto value = json.GetNamedValue(L"d");

            if (value.ValueType() == JsonValueType::Object) {
                auto data = value.GetObject();
                switch (op) {
                case 8: // hello
                {
                    heartbeatInterval = (int32_t)data.GetNamedNumber(L"heartbeat_interval");
                    co_await SendIdentifyAsync(can_resume);
                    break;
                }
                case 2: // ready
                {
                    audioSSRC = (uint32_t)data.GetNamedNumber(L"ssrc");
                    co_await Stage1(data);
                    break;
                }
                case 4: // session description
                {
                    Stage2(data);
                    break;
                }
                case 5: // speaking
                {
                    uint32_t speaking_ssrc = (uint32_t)data.GetNamedNumber(L"ssrc");
                    AudioSource* audio_source = opusDecoder->GetOrCreateDecoder(speaking_ssrc);
                    audio_source->user_id = std::stoll(to_string(data.GetNamedString(L"user_id")));
                    audio_source->is_speaking = data.GetNamedNumber(L"speaking") != 0;

                    break;
                }
                case 13: // client_disconnected
                {
                    uint64_t user_id = std::stoll(to_string(data.GetNamedString(L"user_id")));
                    AudioSource* source = opusDecoder->GetAssociatedAudioSource(user_id, true);

                    if (source != nullptr) {
                        renderer->RemoveAudioSource(source->ssrc);
                        delete source;
                    }

                    break;
                }
                }
            }

            if (value.ValueType() == JsonValueType::String) {
                auto data = value.GetString();
                switch (op) {
                case 6: // heartbeat ack
                    uint32_t now = (uint32_t)duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
                    ws_ping = now - last_heartbeat;
                    wsPingUpdated(*this, (const uint32_t)ws_ping);
                    std::cout << "- WS Ping " << ws_ping << "ms\n";
                    break;
                }
            }

            if (value.ValueType() == JsonValueType::Null) {
                switch (op) {
                case 9:
                {
                    HostName remoteHost{ udpEndpoint.hostname };
                    EndpointPair pair{ nullptr, L"", remoteHost, to_hstring(udpEndpoint.port) };
                    co_await udpSocket.ConnectAsync(pair);

                    Stage2(nullptr);
                    connected(*this, true);
                    break;
                }
                }
            }
        }
        catch (const winrt::hresult_error & ex)
        {
            if (!ws_closed && webSocket != nullptr)
                webSocket.Close();
            std::cout << "ERROR: " << to_string(ex.message()) << "\n";
        }
    }

    IAsyncAction VoiceClient::OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev)
    {
        try
        {
            ws_closed = true;
            std::cout << "WebSocket closed with code " << ev.Code() << " and reason " << to_string(ev.Reason()) << "\n";

            if (ev.Code() == 4006 || ev.Code() == 4009) {
                disconnected(*this, false);
                Close();
            }
            else {
                disconnected(*this, true);
                co_await ReconnectLoop();
            }
        }
        catch (const winrt::hresult_error & ex)
        {
            std::cout << "ERROR: " << to_string(ex.message()) << "\n";
        }
    }

    IAsyncAction VoiceClient::ReconnectLoop()
    {
        bool connected = false;
        uint32_t reconnection_count = 0;

        while (!connected) {
            try {
                Reset();

                std::chrono::seconds timeout = std::min<std::chrono::seconds>(5s * reconnection_count, 30s);
                std::cout << "- Reconnecting in " << timeout.count() << "s!" << std::endl;

                co_await timeout;
                cancel_voice_send = false;
                InitialiseSockets();

                ws_closed = false;
                can_resume = true;

                co_await this->ConnectAsync();
                connected = true;
            }
            catch (const winrt::hresult_error & ex) {
                ws_closed = true;
                std::cout << "ERROR: " << to_string(ex.message()) << "\n";
            }

            reconnection_count += 1;
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

    IOutputStream VoiceClient::GetOutputStream()
    {
        return make_self<VoiceOutputStream>(*this).as<IOutputStream>();
    }

    void VoiceClient::UpdateAudioDevices()
    {
        renderer->Initialise(options.PreferredPlaybackDevice(), options.PreferredRecordingDevice());
        renderer->BeginCapture();
        renderer->BeginRender();
    }

    IAsyncAction VoiceClient::OnUdpHeartbeat(ThreadPoolTimer timer)
    {
        try
        {
            DataWriter writer{ udpSocket.OutputStream() };

            uint64_t now = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
            uint64_t count = keepalive_count;
            keepalive_count = count + 1;
            keepalive_timestamps.insert({ count, now });

            std::cout << "< Sending UDP Heartbeat " << count << "\n";

            writer.WriteUInt64(count);
            co_await writer.StoreAsync();
            writer.DetachStream();
        }
        catch (const winrt::hresult_error & ex)
        {
            std::cout << "ERROR: " << to_string(ex.message()) << "\n";
        }
    }

    void VoiceClient::HandleUdpHeartbeat(uint64_t count)
    {
        uint64_t now = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
        std::cout << "> Got UDP Heartbeat " << count << "!\n";

        auto itr = keepalive_timestamps.find(count);
        if (itr != keepalive_timestamps.end()) {
            uint64_t then = keepalive_timestamps.at(count);
            udp_ping = now - then;

            udpPingUpdated(*this, (const uint32_t)udp_ping);
            keepalive_timestamps.unsafe_erase(count);

            std::cout << "- UDP Ping " << udp_ping << "ms\n";
        }
    }

    IAsyncAction VoiceClient::OnUdpMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs ev)
    {
        try
        {
            auto reader = ev.GetDataReader();

            if (connectionStage == 0) {
                uint8_t buff[70];
                reader.ReadBytes(buff);

                std::string ip{ &buff[4], &buff[64] };
                ip = ip.substr(0, ip.find_first_of('\0'));

                uint16_t port = *(uint16_t*)&buff[68];

                co_await Stage3(ip, port);
            }
            else {
                auto len = reader.UnconsumedBufferLength();
                if (len == 8) {
                    HandleUdpHeartbeat(reader.ReadUInt64());
                }
                else if (len > 13) {
                    // prolly voice data
                    auto data = new uint8_t[len];
                    auto data_view = array_view<uint8_t>(data, data + len);
                    reader.ReadBytes(data_view);

                    ProcessRawPacket(data_view);

                    delete[] data;
                }
            }
        }
        catch (const winrt::hresult_error&)
        {

        }
    }

    IAsyncAction VoiceClient::Stage3(std::string& ip, const uint16_t& port)
    {
        JsonObject data;
        data.SetNamedValue(L"address", JsonValue::CreateStringValue(to_hstring(ip)));
        data.SetNamedValue(L"port", JsonValue::CreateNumberValue(port));
        data.SetNamedValue(L"mode", JsonValue::CreateStringValue(mode.first));

        JsonObject opus;
        opus.SetNamedValue(L"name", JsonValue::CreateStringValue(L"opus"));
        opus.SetNamedValue(L"type", JsonValue::CreateStringValue(L"audio"));
        opus.SetNamedValue(L"priority", JsonValue::CreateNumberValue(1000));
        opus.SetNamedValue(L"payload_type", JsonValue::CreateNumberValue(Rtp::RTP_TYPE_OPUS));

        JsonObject h264;
        h264.SetNamedValue(L"name", JsonValue::CreateStringValue(L"H264"));
        h264.SetNamedValue(L"type", JsonValue::CreateStringValue(L"video"));
        h264.SetNamedValue(L"priority", JsonValue::CreateNumberValue(1001));
        h264.SetNamedValue(L"payload_type", JsonValue::CreateNumberValue(Rtp::RTP_TYPE_H264));
        h264.SetNamedValue(L"rtx_payload_type", JsonValue::CreateNumberValue(Rtp::RTP_TYPE_H264_RTX));

        JsonArray codecs;
        codecs.Append(h264);
        codecs.Append(opus);

        JsonObject protocol_select;
        protocol_select.SetNamedValue(L"protocol", JsonValue::CreateStringValue(L"udp"));
        protocol_select.SetNamedValue(L"data", data);
        protocol_select.SetNamedValue(L"address", JsonValue::CreateStringValue(to_hstring(ip)));
        protocol_select.SetNamedValue(L"port", JsonValue::CreateNumberValue(port));
        protocol_select.SetNamedValue(L"mode", JsonValue::CreateStringValue(mode.first));
        protocol_select.SetNamedValue(L"codecs", codecs);

        JsonObject dispatch;
        dispatch.SetNamedValue(L"op", JsonValue::CreateNumberValue(1));
        dispatch.SetNamedValue(L"d", protocol_select);

        co_await SendJsonPayloadAsync(dispatch);

        JsonObject ssrc_info;
        ssrc_info.SetNamedValue(L"audio_ssrc", JsonValue::CreateNumberValue(audioSSRC));
        ssrc_info.SetNamedValue(L"video_ssrc", JsonValue::CreateNumberValue(0));
        ssrc_info.SetNamedValue(L"rx_ssrc", JsonValue::CreateNumberValue(0));

        JsonObject dispatch2;
        dispatch2.SetNamedValue(L"op", JsonValue::CreateNumberValue(12));
        dispatch2.SetNamedValue(L"d", ssrc_info);

        co_await SendJsonPayloadAsync(dispatch2);
        connectionStage = 1;
    }

    IAsyncAction VoiceClient::SendJsonPayloadAsync(JsonObject& payload)
    {
        try
        {
            if (!ws_closed) {
                DataWriter writer{ webSocket.OutputStream() };
                auto str = payload.Stringify();

                std::cout << "< " << to_string(str) << "\n";

                writer.WriteString(str);
                co_await writer.StoreAsync();
                writer.DetachStream();
            }
        }
        catch (const winrt::hresult_error & ex)
        {
            if (!ws_closed && webSocket != nullptr) {
                webSocket.Close();
                ws_closed = true;
            }

            std::cout << "ERROR: " << to_string(ex.message()) << "\n";
        }

    }

    void VoiceClient::Close()
    {
        if (is_disposed)
            return;

        is_disposed = true;

        Reset();

        if (sodium != nullptr) {
            SecureZeroMemory(sodium, sizeof sodium);
            delete sodium;
            sodium = nullptr;
        }
    }

    void VoiceClient::Reset()
    {
        SAFE_CANCEL(heartbeatTimer);
        SAFE_CANCEL(keepaliveTimer);

        if (renderer != nullptr) {
            renderer->StopCapture();
            renderer->StopRender();
            delete renderer;
            renderer = nullptr;
        }

        cancel_voice_send = true;

        if (voice_thread.joinable())
            voice_thread.join();

        PCMPacket packet;
        while (voice_queue.try_pop(packet))
        {
            delete[] packet.bytes.data();
        }

        if (opusDecoder != nullptr) {
            delete opusDecoder;
            opusDecoder = nullptr;
        }
        
        if (opusEncoder != nullptr) {
            delete opusEncoder;
            opusEncoder = nullptr;
        }

        keepalive_timestamps.clear();

        try {
            SAFE_CLOSE(webSocket);
            SAFE_CLOSE(udpSocket);
        }
        catch (const winrt::hresult_error&) {}
    }

    VoiceClient::~VoiceClient()
    {
        Close();
    }
}

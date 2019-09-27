#pragma once

#include "VoiceClient.g.h"
#include "SodiumWrapper.h"
#include "ConnectionEndpoint.h"
#include "AudioFormat.h"
#include "AudioRenderer.h"
#include "OpusDecoder.h"
#include "OpusEncoder.h"
#include "H264Decoder.h"

#include <opus.h>
#include <sodium.h>
#include <string>
#include <iostream>
#include <chrono>
#include <sstream>
#include <thread>
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
using namespace winrt::Unicord::Universal::Voice::Render;
using namespace winrt::Unicord::Universal::Voice::Encode;
using namespace winrt::Unicord::Universal::Voice::Decode;

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

        uint32_t WebSocketPing()
        {
            return ws_ping;
        }

        uint32_t UdpSocketPing()
        {
            return udp_ping;
        }

        winrt::event_token WebSocketPingUpdated(Windows::Foundation::EventHandler<uint32_t> const& handler)
        {
            return wsPingUpdated.add(handler);
        }

        void WebSocketPingUpdated(winrt::event_token const& token) noexcept
        {
            wsPingUpdated.remove(token);
        }

        winrt::event_token UdpSocketPingUpdated(Windows::Foundation::EventHandler<uint32_t> const& handler)
        {
            return udpPingUpdated.add(handler);
        }

        void UdpSocketPingUpdated(winrt::event_token const& token) noexcept
        {
            udpPingUpdated.remove(token);
        }

        winrt::event_token Connected(Windows::Foundation::EventHandler<bool> const& handler)
        {
            return connected.add(handler);
        }

        void Connected(winrt::event_token const& token) noexcept
        {
            connected.remove(token);
        }

        winrt::event_token Disconnected(Windows::Foundation::EventHandler<bool> const& handler)
        {
            return disconnected.add(handler);
        }

        void Disconnected(winrt::event_token const& token) noexcept
        {
            disconnected.remove(token);
        }

        event_token VideoDataRecieved(EventHandler<Voice::VideoEventArgs> const& handler)
        {
            return videoRecieved.add(handler);
        }

        void VideoDataRecieved(event_token const& token) noexcept
        {
            videoRecieved.remove(token);
        }

        IAsyncAction ConnectAsync();
        IAsyncAction SendSpeakingAsync(bool speaking);
        IOutputStream GetOutputStream();
        void UpdateAudioDevices();
        void Close();

        bool Muted();
        void Muted(bool value);
        bool Deafened();
        void Deafened(bool value);

        VoicePacket PreparePacket(array_view<uint8_t> pcm, bool silence = false, bool is_float = false);
        void EnqueuePacket(PCMPacket packet);

        ~VoiceClient();
    private:
        void InitialiseSockets();
        IAsyncAction SendIdentifyAsync(bool isResume = false);
        IAsyncAction SendJsonPayloadAsync(JsonObject& payload);
        IAsyncAction Stage1(JsonObject obj);
        void Stage2(JsonObject obj);
        IAsyncAction Stage3(std::string& ip, const uint16_t& port);

        void VoiceSendLoop();

        void ProcessRawPacket(array_view<uint8_t> data);
        bool ProcessIncomingPacket(array_view<const uint8_t> data, std::vector<std::vector<uint8_t>>& pcm, AudioSource** source);

        IAsyncAction OnWsHeartbeat(ThreadPoolTimer sender);
        IAsyncAction OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev);
        IAsyncAction OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev);
        IAsyncAction ReconnectLoop();

        IAsyncAction OnUdpHeartbeat(ThreadPoolTimer sender);
        IAsyncAction OnUdpMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs ev);
        void HandleUdpHeartbeat(uint64_t reader);
        void Reset();

        winrt::event<EventHandler<bool>> connected;
        winrt::event<EventHandler<bool>> disconnected;
        winrt::event<EventHandler<uint32_t>> wsPingUpdated;
        winrt::event<EventHandler<uint32_t>> udpPingUpdated;
        winrt::event<EventHandler<Voice::VideoEventArgs>> videoRecieved;

        VoiceClientOptions options{ nullptr };
        MessageWebSocket webSocket{ nullptr };
        DatagramSocket udpSocket{ nullptr };
        ThreadPoolTimer heartbeatTimer{ nullptr };
        ThreadPoolTimer keepaliveTimer{ nullptr };

        std::pair<hstring, EncryptionMode> mode;
        SodiumWrapper* sodium = nullptr;
        Encode::OpusEncoder* opusEncoder = nullptr;
        Decode::OpusDecoder* opusDecoder = nullptr;
        Decode::H264Decoder* h264Decoder = nullptr;
        AudioRenderer* renderer = nullptr;
        ConnectionEndpoint wsEndpoint;
        ConnectionEndpoint udpEndpoint;

        bool is_speaking = false;
        bool is_muted = false;
        bool is_deafened = false;

        bool ws_closed = true;
        bool can_resume = false;

        uint16_t audioSequence = 0;
        uint32_t audioSSRC = 0;
        uint32_t audioTimestamp = 0;
        uint32_t nonce = 0;

        uint32_t heartbeatInterval = 0;
        uint32_t connectionStage = 0;

        volatile uint32_t ws_ping = 0;
        volatile uint32_t last_heartbeat = 0;

        volatile uint32_t udp_ping = 0;
        volatile uint64_t keepalive_count = 0;

        concurrency::concurrent_queue<PCMPacket> voice_queue;
        concurrency::concurrent_unordered_map<uint64_t, uint64_t> keepalive_timestamps;

        volatile bool cancel_voice_send = false;
        std::thread voice_thread;

        bool is_disposed = false;
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

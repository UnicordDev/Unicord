#include "pch.h"
#include "VoiceClient.h"
#include "VoiceClient.g.cpp"
#include <opus.h>
#include <sodium.h>
#include <debugapi.h>
#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Storage.Streams.h>

using namespace winrt::Windows::Networking::Sockets;

namespace winrt::Unicord::Universal::Voice::implementation
{
	winrt::hstring VoiceClient::OpusVersion()
	{
		auto strptr = opus_get_version_string();
		return to_hstring(strptr);
	}

	winrt::hstring VoiceClient::SodiumVersion()
	{
		auto strptr = sodium_version_string();
		return to_hstring(strptr);
	}

	VoiceClient::VoiceClient(Unicord::Universal::Voice::VoiceClientOptions const& options)
	{
		if (options.Token().size() == 0 && options.ChannelId() == 0)
		{
			throw hresult_invalid_argument();
		}

		web_socket = MessageWebSocket();
		udp_socket = DatagramSocket();

		web_socket.MessageReceived({ this, &VoiceClient::OnWsMessage });
		web_socket.Closed({ this, &VoiceClient::OnWsClosed });
	}

	winrt::Windows::Foundation::IAsyncAction VoiceClient::ConnectAsync()
	{
		
	}

	void VoiceClient::OnWsMessage(IWebSocket socket, MessageWebSocketMessageReceivedEventArgs ev)
	{
		try
		{
			auto reader = ev.GetDataReader();
			auto data = reader.ReadString(reader.UnconsumedBufferLength());
			reader.Close();

			auto json = Windows::Data::Json::JsonObject::Parse(data);
			auto op = json.GetNamedNumber(L"op");
			
		}
		catch (const std::exception& ex)
		{
			OutputDebugStringA(ex.what());
		}
	}

	void VoiceClient::OnWsClosed(IWebSocket socket, WebSocketClosedEventArgs ev)
	{

	}

	void VoiceClient::LogMessage(std::string str)
	{
		OutputDebugStringA(str.c_str());
	}

	void VoiceClient::Close()
	{
		web_socket.Close();
		udp_socket.Close();
	}
}

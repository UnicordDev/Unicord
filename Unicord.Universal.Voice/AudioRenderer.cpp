#include "pch.h"
#include "AudioRenderer.h"
#include "VoiceClient.h"

using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Media;
using namespace winrt::Windows::Media::Audio;
using namespace winrt::Windows::Media::Capture;
using namespace winrt::Windows::Media::Devices;
using namespace winrt::Windows::Media::Render;
using namespace winrt::Windows::Media::MediaProperties;
using namespace winrt::Windows::Devices::Enumeration;

namespace winrt::Unicord::Universal::Voice::Render
{
	AudioRenderer::AudioRenderer(winrt::Unicord::Universal::Voice::implementation::VoiceClient* client)
	{
		voice_client = client; 
		buffer_length = client->audio_format.CalculateSampleSize(20);
		pcm_buffer = new uint8_t[buffer_length]{ 0 };
	}

	IAsyncAction AudioRenderer::Initialise(hstring preferred_render_device_id, hstring preferred_capture_device_id)
	{
		hstring render_device_id = L"";
		hstring capture_device_id = L"";
		if (!preferred_render_device_id.empty()) {
			render_device_id = preferred_render_device_id;
		}
		else {
			render_device_id = MediaDevice::GetDefaultAudioRenderId(AudioDeviceRole::Default);
		}

		if (!preferred_capture_device_id.empty()) {
			capture_device_id = preferred_capture_device_id;
		}
		else {
			capture_device_id = MediaDevice::GetDefaultAudioCaptureId(AudioDeviceRole::Default);
		}

		// this is slightly painful ngl

		DeviceInformation render_device_info = co_await DeviceInformation::CreateFromIdAsync(render_device_id);
		DeviceInformation capture_device_info = co_await DeviceInformation::CreateFromIdAsync(capture_device_id);
		AudioEncodingProperties audio_format = AudioEncodingProperties::CreatePcm(voice_client->audio_format.sample_rate, 1, 16);

		AudioGraphSettings settings{ AudioRenderCategory::Media };
		settings.PrimaryRenderDevice(render_device_info);

		auto result = co_await AudioGraph::CreateAsync(settings);
		if (result.Status() != AudioGraphCreationStatus::Success) { // why not just throw an exception for me?
			throw hresult_error(E_FAIL, L"Failed to initialize audio output device");
		}

		render_graph = result.Graph();

		auto render_node_result = co_await render_graph.CreateDeviceOutputNodeAsync();
		if (render_node_result.Status() != AudioDeviceNodeCreationStatus::Success) {
			throw hresult_error(E_FAIL, L"Failed to initialize audio output device " + to_hstring((int32_t)render_node_result.Status()));
		}

		render_node = render_node_result.DeviceOutputNode();

		settings = AudioGraphSettings(AudioRenderCategory::Media);
		settings.EncodingProperties(audio_format);

		result = co_await AudioGraph::CreateAsync(settings);
		if (result.Status() != AudioGraphCreationStatus::Success) { // why not just throw an exception for me?
			throw hresult_error(E_FAIL, L"Failed to initialize audio input device");
		}

		capture_graph = result.Graph();
		capture_graph.QuantumStarted({ this, &AudioRenderer::OnQuantumStarted });

		auto capture_node_result = co_await capture_graph.CreateDeviceInputNodeAsync(MediaCategory::Media, audio_format, capture_device_info);
		if (capture_node_result.Status() != AudioDeviceNodeCreationStatus::Success) {
			throw hresult_error(E_FAIL, L"Failed to initialize audio input device");
		}

		capture_node = capture_node_result.DeviceInputNode();
		capture_submix = capture_graph.CreateSubmixNode(audio_format);
		capture_node.AddOutgoingConnection(capture_submix);
		capture_frame_node = capture_graph.CreateFrameOutputNode(audio_format);
		capture_submix.AddOutgoingConnection(capture_frame_node);
	}

	void AudioRenderer::ProcessIncomingPacket(std::vector<uint8_t> packet, AudioSource sender)
	{
		std::unique_lock lock(output_mutex);

		AudioFrameInputNode input_node{ nullptr };
		AudioEncodingProperties properties{ nullptr };

		auto mode_iter = input_nodes.find(sender.ssrc);
		if (mode_iter == input_nodes.end()) {
			properties = AudioEncodingProperties::CreatePcm(sender.format.sample_rate, sender.format.channel_count, 16);

			input_node = render_graph.CreateFrameInputNode(properties);
			input_node.AddOutgoingConnection(render_node);;
			input_nodes.insert(std::pair(sender.ssrc, input_node));
		}
		else {
			input_node = input_nodes.at(sender.ssrc);
			properties = input_node.EncodingProperties();
			if (properties.ChannelCount() != sender.format.channel_count)
			{
				input_node.RemoveOutgoingConnection(render_node);
				input_node.Close();

				properties = AudioEncodingProperties::CreatePcm(sender.format.sample_rate, sender.format.channel_count, 16);
				input_node = render_graph.CreateFrameInputNode(properties);
				input_node.AddOutgoingConnection(render_node);
				input_nodes.insert(std::pair(sender.ssrc, input_node));
			}
		}

		try
		{
			AudioFrame frame(packet.size());
			AudioBuffer buff = frame.LockBuffer(AudioBufferAccessMode::Write);
			IMemoryBufferReference buffer_reference = buff.CreateReference();
			com_ptr<IMemoryBufferByteAccess> byte_buffer_access = buffer_reference.as<IMemoryBufferByteAccess>();

			uint8_t* data_byte_ptr = nullptr;
			uint32_t byte_read = 0;

			winrt::check_hresult(byte_buffer_access->GetBuffer(&data_byte_ptr, &byte_read));
			std::copy(packet.begin(), packet.end(), data_byte_ptr);

			buffer_reference.Close();
			buff.Close();

			input_node.AddFrame(frame);
		}
		catch (const winrt::hresult_illegal_method_call&)
		{
			input_node.DiscardQueuedFrames();
		}
	}

	void AudioRenderer::BeginCapture()
	{
		capture_graph.Start();
	}

	void AudioRenderer::BeginRender()
	{
		render_graph.Start();
	}

	AudioEncodingProperties AudioRenderer::GetCaptureProperties()
	{
		return capture_frame_node.EncodingProperties();
	}

	void AudioRenderer::OnQuantumStarted(Windows::Media::Audio::AudioGraph graph, Windows::Foundation::IInspectable const &)
	{
		try
		{
			AudioFrame frame = capture_frame_node.GetFrame();
			AudioBuffer audio_buff = frame.LockBuffer(AudioBufferAccessMode::Read);
			IMemoryBufferReference buffer_reference = audio_buff.CreateReference();
			com_ptr<IMemoryBufferByteAccess> byte_buffer_access = buffer_reference.as<IMemoryBufferByteAccess>();

			uint8_t* buff = nullptr;
			uint32_t buffer_size = 0;
			winrt::check_hresult(byte_buffer_access->GetBuffer(&buff, &buffer_size));

			auto packet = voice_client->PreparePacket(array_view<uint8_t>(buff, buff + buffer_size), false, true);
			voice_client->EnqueuePacket(packet);

			buffer_reference.Close();
			audio_buff.Close();
			frame.Close();
		}
		catch (const std::exception&)
		{

		}
	}

	AudioRenderer::~AudioRenderer()
	{
	}
}
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

    void AudioRenderer::Initialise(hstring preferred_render_device_id, hstring preferred_capture_device_id)
    {
        hstring render_device_id = L"";
        hstring capture_device_id = L"";
        if (!preferred_render_device_id.empty()) {
            render_device_id = preferred_render_device_id;
        }

        if (!preferred_capture_device_id.empty()) {
            capture_device_id = preferred_capture_device_id;
        }

        std::unique_lock in_lock(input_mutex);
        std::unique_lock out_lock(output_mutex);

        AudioEncodingProperties audio_format = AudioEncodingProperties::CreatePcm(voice_client->audio_format.sample_rate, voice_client->audio_format.channel_count, 16);
        AudioGraphSettings settings{ AudioRenderCategory::Communications };
        settings.EncodingProperties(audio_format);

        // this is slightly painful ngl
        if (!render_device_id.empty()) {
            DeviceInformation render_device_info = DeviceInformation::CreateFromIdAsync(render_device_id).get();
            settings.PrimaryRenderDevice(render_device_info);
        }

        if (render_graph == nullptr) {
            auto result = AudioGraph::CreateAsync(settings).get();
            if (result.Status() != AudioGraphCreationStatus::Success) { // why not just throw an exception for me?
                throw hresult_error(E_FAIL, L"Failed to initialize audio output device");
            }
            render_graph = result.Graph();
        }
        else {
            render_graph.Stop();
        }

        if (render_submix_node == nullptr) {
            render_submix_node = render_graph.CreateSubmixNode();
        }

        if (render_node != nullptr) {
            render_submix_node.RemoveOutgoingConnection(render_node);
            render_node.Close();
            render_node = nullptr;
        }

        auto render_node_result = render_graph.CreateDeviceOutputNodeAsync().get();
        if (render_node_result.Status() != AudioDeviceNodeCreationStatus::Success) {
            throw hresult_error(E_FAIL, L"Failed to initialize audio output device " + to_hstring((int32_t)render_node_result.Status()));
        }

        render_node = render_node_result.DeviceOutputNode();
        render_submix_node.AddOutgoingConnection(render_node);

        if (capture_graph == nullptr) {
            auto result = AudioGraph::CreateAsync(settings).get();
            if (result.Status() != AudioGraphCreationStatus::Success) {
                return;
            }

            capture_graph = result.Graph();
            capture_graph.QuantumStarted({ this, &AudioRenderer::OnQuantumStarted });
        }
        else {
            capture_graph.Stop();
        }

        if (capture_frame_node == nullptr) {
            capture_frame_node = capture_graph.CreateFrameOutputNode(audio_format);
        }

        if (capture_node != nullptr) {
            capture_node.RemoveOutgoingConnection(capture_frame_node);
            capture_node.Close();
            capture_node = nullptr;
        }

        CreateAudioDeviceInputNodeResult capture_node_result{ nullptr };

        if (!capture_device_id.empty()) {
            DeviceInformation capture_device_info = DeviceInformation::CreateFromIdAsync(capture_device_id).get();
            capture_node_result = capture_graph.CreateDeviceInputNodeAsync(MediaCategory::Communications, audio_format, capture_device_info).get();
        }
        else {
            capture_node_result = capture_graph.CreateDeviceInputNodeAsync(MediaCategory::Communications, audio_format).get();
        }

        if (capture_node_result.Status() == AudioDeviceNodeCreationStatus::Success) {
            capture_node = capture_node_result.DeviceInputNode();
            capture_node.AddOutgoingConnection(capture_frame_node);
        }
    }

    void AudioRenderer::ProcessIncomingPacket(std::vector<uint8_t> packet, AudioSource* sender)
    {
        std::unique_lock lock(output_mutex);

        AudioFrameInputNode input_node{ nullptr };
        AudioEncodingProperties properties{ nullptr };

        auto mode_iter = input_nodes.find(sender->ssrc);
        if (mode_iter == input_nodes.end()) {
            CreateAudioInputNode(properties, sender, input_node);
        }
        else {
            input_node = input_nodes.at(sender->ssrc);
            properties = input_node.EncodingProperties();
            if (properties.ChannelCount() != sender->format.channel_count)
            {
                input_nodes.unsafe_erase(sender->ssrc);
                input_node.RemoveOutgoingConnection(render_submix_node);
                input_node.Close();

                CreateAudioInputNode(properties, sender, input_node);
            }
        }

        try
        {
            AudioFrame frame((uint32_t)packet.size());
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

    void AudioRenderer::CreateAudioInputNode(AudioEncodingProperties &properties, AudioSource * sender, AudioFrameInputNode &input_node)
    {
        properties = AudioEncodingProperties::CreatePcm(sender->format.sample_rate, sender->format.channel_count, 16);
        input_node = render_graph.CreateFrameInputNode(properties);
        input_node.AddOutgoingConnection(render_submix_node);
        input_node.Start();

        input_nodes.insert(std::pair(sender->ssrc, input_node));
    }

    void AudioRenderer::BeginCapture()
    {
        if (capture_graph != nullptr)
            capture_graph.Start();
    }

    void AudioRenderer::BeginRender()
    {
        if (render_graph != nullptr)
            render_graph.Start();
    }

    void AudioRenderer::StopCapture()
    {
        if (capture_graph != nullptr)
            capture_graph.Stop();
    }

    void AudioRenderer::StopRender()
    {
        if (render_graph != nullptr)
            render_graph.Stop();
    }

    AudioEncodingProperties AudioRenderer::GetRenderProperties()
    {
        return render_submix_node.EncodingProperties();
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

            if (buffer_size != 0) {

                uint8_t* new_buff = new uint8_t[buffer_size];
                std::copy(buff, buff + buffer_size, new_buff);

                PCMPacket packet(gsl::make_span(new_buff, buffer_size), voice_client->audio_format.CalculateSampleDurationF(buffer_size));
                packet.is_float = true;

                voice_client->EnqueuePacket(packet);
            }

            buffer_reference.Close();
            audio_buff.Close();
        }
        catch (const std::exception&)
        {

        }
    }

    AudioRenderer::~AudioRenderer()
    {
        std::unique_lock in_lock(input_mutex);
        std::unique_lock out_lock(output_mutex);

        std::cout << "Freeing AudioRenderer\n";

        for each (auto node in input_nodes) {
            node.second.Close();
        }

        render_submix_node.Close();
        render_submix_node = nullptr;
        render_node.Close();
        render_node = nullptr;
        render_graph.Close();
        render_graph = nullptr;

        input_nodes.clear();

        capture_frame_node.Close();
        capture_frame_node = nullptr;
        capture_node.Close();
        capture_node = nullptr;
        capture_graph.Close();
        capture_graph = nullptr;

        delete[] pcm_buffer;
        voice_client = nullptr;
    }
}
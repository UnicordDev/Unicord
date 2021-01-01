#pragma once

#include <concurrent_unordered_map.h>
#include <winrt/Windows.Media.h>
#include <winrt/Windows.Media.Audio.h>
#include <winrt/Windows.Media.Devices.h>
#include <winrt/Windows.Media.Capture.h>
#include <winrt/Windows.Media.MediaProperties.h>
#include <winrt/Windows.Devices.h>
#include <winrt/Windows.Devices.Enumeration.h>
#include "AudioFormat.h"

#define SAFE_CLOSE(x)   \
    if (x != nullptr) { \
        x.Close();      \
        x = nullptr;    \
    }

struct __declspec(uuid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")) __declspec(novtable) IMemoryBufferByteAccess : ::IUnknown {
    virtual HRESULT __stdcall GetBuffer(uint8_t** value, uint32_t* capacity) = 0;
};


namespace winrt::Unicord::Universal::Voice::implementation {
    struct VoiceClient;
}
using namespace winrt::Unicord::Universal::Voice::Interop;
using namespace winrt::Unicord::Universal::Voice::implementation;


namespace winrt::Unicord::Universal::Voice::Render {
    class AudioRenderer {
    public:
        AudioRenderer(implementation::VoiceClient* client);

        void Initialise(hstring preferred_render_device_id, hstring preferred_capture_device_id);
        void ProcessIncomingPacket(std::vector<uint8_t> packet, AudioSource* sender);

        void CreateAudioInputNode(winrt::Windows::Media::MediaProperties::AudioEncodingProperties& properties, winrt::Unicord::Universal::Voice::Interop::AudioSource* sender, winrt::Windows::Media::Audio::AudioFrameInputNode& input_node);

        void BeginCapture();
        void BeginRender();

        void StopCapture();
        void StopRender();

        Windows::Media::MediaProperties::AudioEncodingProperties GetCaptureProperties();
        Windows::Media::MediaProperties::AudioEncodingProperties GetRenderProperties();

        ~AudioRenderer();

    private:
        implementation::VoiceClient* voice_client;
        std::mutex output_mutex;
        Windows::Media::Audio::AudioGraph render_graph{ nullptr };
        Windows::Media::Audio::AudioDeviceOutputNode render_node{ nullptr };
        Windows::Media::Audio::AudioSubmixNode render_submix_node{ nullptr };
        concurrency::concurrent_unordered_map<uint32_t, Windows::Media::Audio::AudioFrameInputNode> input_nodes;

        std::mutex input_mutex;
        Windows::Media::Audio::AudioGraph capture_graph{ nullptr };
        Windows::Media::Audio::AudioDeviceInputNode capture_node{ nullptr };
        Windows::Media::Audio::AudioFrameOutputNode capture_frame_node{ nullptr };

        uint8_t* pcm_buffer = nullptr;
        size_t buffer_length = 0;
        size_t consumed_buffer_length = 0;

        void OnQuantumStarted(Windows::Media::Audio::AudioGraph graph, Windows::Foundation::IInspectable const&);
    };
}
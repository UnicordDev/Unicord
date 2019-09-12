#pragma once
#include "VoiceClient.h"
#include "VoiceOutputStream.g.h"

namespace winrt::Unicord::Universal::Voice::implementation
{
    struct VoiceOutputStream : VoiceOutputStreamT<VoiceOutputStream>
    {
        VoiceOutputStream() = default;
        VoiceOutputStream(Unicord::Universal::Voice::VoiceClient const& client);

        Windows::Foundation::IAsyncOperationWithProgress<uint32_t, uint32_t> WriteAsync(Windows::Storage::Streams::IBuffer buffer);
        Windows::Foundation::IAsyncOperation<bool> FlushAsync();
        void Close();

    private:
        winrt::Unicord::Universal::Voice::implementation::VoiceClient* client = nullptr;
        uint8_t* pcm_buffer = nullptr;
        size_t buffer_length = 0;
        size_t consumed_buffer_length = 0;
    };
}
namespace winrt::Unicord::Universal::Voice::factory_implementation
{
    struct VoiceOutputStream : VoiceOutputStreamT<VoiceOutputStream, implementation::VoiceOutputStream>
    {
    };
}

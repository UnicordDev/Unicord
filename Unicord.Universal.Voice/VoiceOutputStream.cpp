#include "pch.h"
#include "VoiceOutputStream.h"
#include "VoiceOutputStream.g.cpp"

namespace winrt::Unicord::Universal::Voice::implementation
{
	VoiceOutputStream::VoiceOutputStream(Unicord::Universal::Voice::VoiceClient const& client)
	{
		this->client = get_self<implementation::VoiceClient>(client);
		this->buffer_length = this->client->audio_format.CalculateSampleSize(20);
		this->pcm_buffer = new uint8_t[this->buffer_length];
		this->consumed_buffer_length = 0;
	}

	Windows::Foundation::IAsyncOperationWithProgress<uint32_t, uint32_t> VoiceOutputStream::WriteAsync(Windows::Storage::Streams::IBuffer buffer)
	{
		auto buff = buffer.data();
		auto buffer_size = buffer.Length();

		auto remaining = buffer_size;
		auto index = 0;
		while (remaining > 0) {
			auto len = min(buffer_length - consumed_buffer_length, remaining);
			auto tgt = array_view<uint8_t>(pcm_buffer + consumed_buffer_length, pcm_buffer + buffer_length);
			auto src = array_view<uint8_t>(buff + index, buff + index + len);

			std::copy(src.begin(), src.end(), tgt.data());

			consumed_buffer_length += len;
			index += len;
			remaining -= len;

			if (consumed_buffer_length == buffer_length) {
				consumed_buffer_length = 0;

				auto packet = client->PreparePacket(array_view<uint8_t>(pcm_buffer, pcm_buffer + buffer_length));
				client->EnqueuePacket(VoicePacket(packet, 20));
			}
		}

		co_return buffer_size;
	}

	Windows::Foundation::IAsyncOperation<bool> VoiceOutputStream::FlushAsync()
	{		
		std::fill(pcm_buffer + consumed_buffer_length, pcm_buffer + buffer_length, 0);

		auto packet = client->PreparePacket(array_view<uint8_t>(pcm_buffer, pcm_buffer + buffer_length));
		client->EnqueuePacket(VoicePacket(packet, 20));

		co_return true;
	}

	void VoiceOutputStream::Close()
	{
		delete[] pcm_buffer;
	}
}

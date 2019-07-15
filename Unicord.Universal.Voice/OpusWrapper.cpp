#include "pch.h"
#include "OpusWrapper.h"

namespace winrt::Unicord::Universal::Voice::Interop
{
	OpusWrapper::OpusWrapper(AudioFormat format)
	{
		int error;
		this->audio_format = format;
		this->opus_encoder = opus_encoder_create(format.sample_rate, format.channel_count, (int)format.application, &error);
		check_opus_error(error, L"Failed to instantate Opus encoder");

		int signal = OPUS_AUTO;
		switch (format.application)
		{
		case VoiceApplication::voip:
			signal = OPUS_SIGNAL_VOICE;
			break;

		case VoiceApplication::music:
			signal = OPUS_SIGNAL_MUSIC;
			break;
		}

		check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_SIGNAL_REQUEST, signal), L"Failed to set signal.");
		check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_PACKET_LOSS_PERC_REQUEST, 15), L"Failed to set packet loss percent.");
		check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_INBAND_FEC_REQUEST, 1), L"Failed to set fec.");
		check_opus_error(opus_encoder_ctl(this->opus_encoder, OPUS_SET_BITRATE_REQUEST, 131072), L"Failed to set bitrate.");
	}

	size_t OpusWrapper::Encode(array_view<uint8_t> pcm, array_view<uint8_t> target)
	{
		auto duration = audio_format.CalculateSampleDuration(pcm.size());
		auto frame_size = audio_format.CalculateFrameSize(duration);
		auto sample_size = audio_format.CalculateSampleSize(duration);

		if (pcm.size() != sample_size)
			throw winrt::hresult_invalid_argument(L"Invalid PCM sample size.");
		
		int length = opus_encode(opus_encoder, (int16_t*)(pcm.data()), frame_size, target.data(), target.size());
		if (length < 0) {
			check_opus_error(length, L"Could not encode PCM to opus!");
		}
		
		return (size_t)length;
	}

	void OpusWrapper::Decode(AudioSource decoder, array_view<uint8_t> opus, array_view<uint8_t> &target, bool fec, AudioFormat & format)
	{
		auto frames = opus_packet_get_nb_frames(opus.data(), opus.size());
		auto samples_per_frame = opus_packet_get_samples_per_frame(opus.data(), format.sample_rate);
		auto channels = opus_packet_get_nb_channels(opus.data());

		if (decoder.format.channel_count != channels || !decoder.IsInitialised()) {
			format.channel_count = channels;
			decoder.Initialise(format);
		}

		auto sample_count = opus_decode(decoder.decoder, opus.data(), opus.size(), (int16_t*)target.data(), frames * samples_per_frame, fec);
		if (sample_count < 0) {
			check_opus_error(sample_count, L"Could not decoder opus to PCM!");
		}

		auto sample_size = format.CalculateSampleSize(sample_count);
		target = array_view(target.data(), target.data() + sample_size);
	}

	void OpusWrapper::ProcessPacketLoss(AudioSource decoder, int32_t frameSize, array_view<uint8_t> target)
	{

	}

	AudioSource* OpusWrapper::GetOrCreateDecoder(uint8_t ssrc)
	{
		auto itr = opus_decoders.find(ssrc);
		if (itr == opus_decoders.end()) {
			auto source = new AudioSource(ssrc);		
			opus_decoders[ssrc] = source;
			return source;
		}
		else {
			return opus_decoders.at(ssrc);
		}
	}

	int32_t OpusWrapper::GetLastPacketSampleCount(OpusDecoder* decoder)
	{
		int32_t count;
		opus_decoder_ctl(decoder, OPUS_GET_LAST_PACKET_DURATION_REQUEST, &count);

		return count;
	}

	OpusWrapper::~OpusWrapper()
	{
		if (this->opus_encoder != nullptr)
		{
			opus_encoder_destroy(this->opus_encoder);
		}

		for each (auto decoder in this->opus_decoders)
		{
			opus_decoder_destroy(decoder.second->decoder);
			delete decoder.second;
		}
	}

	void OpusWrapper::check_opus_error(int error, winrt::hstring message)
	{
		switch (error)
		{
		case OPUS_BAD_ARG:
		case OPUS_BUFFER_TOO_SMALL:
		case OPUS_INVALID_PACKET:
			throw winrt::hresult_invalid_argument(message);
		case OPUS_INTERNAL_ERROR:
		case OPUS_INVALID_STATE:
			throw winrt::hresult_error(E_UNEXPECTED, message);
		case OPUS_UNIMPLEMENTED:
			throw winrt::hresult_not_implemented(message);
		case OPUS_ALLOC_FAIL:
			throw winrt::hresult_error(E_OUTOFMEMORY, message);
		default:
			return;
		}
	}
}
#pragma once
#include <winrt/Windows.Data.Json.h>

using namespace winrt::Windows::Data::Json;

namespace winrt::Unicord::Universal::Voice::Interop
{
	enum EncryptionMode {
		/// <summary>
		/// The nonce is an incrementing uint32 value. It is encoded as big endian value at the beginning of the nonce buffer. The 4 bytes are also appended at the end of the packet.
		/// </summary>
		XSalsa20_Poly1305_Lite,

		/// <summary>
		/// The nonce consists of random bytes. It is appended at the end of a packet.
		/// </summary>
		XSalsa20_Poly1305_Suffix,

		/// <summary>
		/// The nonce consists of the RTP header. Nothing is appended to the packet.
		/// </summary>
		XSalsa20_Poly1305

	};

    struct SodiumWrapper
    {
		SodiumWrapper() = default;
        SodiumWrapper(array_view<uint8_t> key_view, EncryptionMode mode);

		static EncryptionMode GetEncryptionMode(hstring name);
		static std::pair<hstring, EncryptionMode> SelectEncryptionMode(JsonArray available_modes);
	private:
		static std::map<hstring, EncryptionMode> getEncryptionMap();

		size_t key_length;
		size_t nonce_length;
		size_t mac_length;

		EncryptionMode mode;

		uint8_t* key;
		uint8_t* buffer;
    };
}

#include "pch.h"
#include "SodiumWrapper.h"
#include <map>
#include <sodium.h>
#include <sodium/randombytes.h>

using namespace winrt::Windows::Data::Json;

namespace winrt::Unicord::Universal::Voice::Interop
{
	SodiumWrapper::SodiumWrapper(array_view<uint8_t> key_view, EncryptionMode selected_mode)
    {
		sodium_init();

		key_length = crypto_secretbox_xsalsa20poly1305_keybytes();
		nonce_length = crypto_secretbox_xsalsa20poly1305_noncebytes();
		mac_length = crypto_secretbox_xsalsa20poly1305_macbytes();
		mode = selected_mode;

		if (key_view.size() != key_length)
			throw hresult_invalid_argument();

		key = new uint8_t[key_view.size()];
		buffer = new uint8_t[nonce_length];

		memcpy_s(key, key_length, key_view.data(), key_view.size());
    }



	EncryptionMode SodiumWrapper::GetEncryptionMode(hstring name)
	{
		auto mode_map = SodiumWrapper::getEncryptionMap();
		return mode_map[name];
	}
	
	std::pair<hstring, EncryptionMode> SodiumWrapper::SelectEncryptionMode(JsonArray available_modes)
	{
		auto mode_map = getEncryptionMap();

		for (auto mode : available_modes) {
			auto result = mode_map.find(mode.GetString());
			if (result != mode_map.end()) {
				return std::make_pair(result->first, result->second);
			}
		}

		throw hresult_not_implemented();
	}

	std::map<hstring, EncryptionMode> SodiumWrapper::getEncryptionMap()
	{
		std::map<hstring, EncryptionMode> mode_map;
		mode_map[L"xsalsa20_poly1305"] = EncryptionMode::XSalsa20_Poly1305;
		mode_map[L"xsalsa20_poly1305_suffix"] = EncryptionMode::XSalsa20_Poly1305_Suffix;
		mode_map[L"xsalsa20_poly1305_lite"] = EncryptionMode::XSalsa20_Poly1305_Lite;

		return mode_map;
	}
}

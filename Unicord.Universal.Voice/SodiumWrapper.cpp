#include "pch.h"
#include "SodiumWrapper.h"
#include "Rtp.h"
#include <map>
#include <sodium.h>
#include <sodium/randombytes.h>

using namespace winrt::Windows::Data::Json;

namespace winrt::Unicord::Universal::Voice::Interop
{
	SodiumWrapper::SodiumWrapper(array_view<uint8_t> key_view, EncryptionMode selected_mode)
    {
		key_length = crypto_secretbox_xsalsa20poly1305_keybytes();
		nonce_length = crypto_secretbox_xsalsa20poly1305_noncebytes();
		mac_length = crypto_secretbox_xsalsa20poly1305_macbytes();
		mode = selected_mode;

		if (key_view.size() != key_length)
			throw hresult_invalid_argument();

		key = new uint8_t[key_view.size()];

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

	void SodiumWrapper::GenerateNonce(array_view<uint8_t> rtp_header, uint8_t target[], size_t target_size)
	{
		if (target_size != nonce_length) {
			throw hresult_invalid_argument(L"Target size incorrect!");
		}

		std::copy(rtp_header.begin(), rtp_header.end(), &target[0]);
	}

	void SodiumWrapper::GenerateNonce(uint8_t target[], size_t target_size)
	{
		if (target_size != nonce_length) {
			throw hresult_invalid_argument(L"Target size incorrect!");
		}

		randombytes_buf(target, crypto_secretbox_xsalsa20poly1305_NONCEBYTES);
	}

	void SodiumWrapper::GenerateNonce(uint32_t nonce, uint8_t target[], size_t target_size)
	{
		if (target_size != nonce_length) {
			throw hresult_invalid_argument(L"Target size incorrect!");
		}

		std::reverse_copy((uint8_t*)&nonce, (uint8_t*)&nonce + sizeof nonce, target);
	}

	void SodiumWrapper::Encrypt(array_view<uint8_t> source, array_view<uint8_t> nonce, uint8_t target[], size_t target_size)
	{
		if (nonce.size() != nonce_length)
			throw hresult_invalid_argument(L"Invalid nonce size");

		if (target_size != mac_length + source.size())
			throw hresult_invalid_argument(L"Invalid target size");

		int result = crypto_secretbox_easy(target, source.begin(), source.size(), nonce.begin(), key);
		if (result != 0) {
			throw hresult_error(E_FAIL, L"Encryption failed!");
		}
	}

	void SodiumWrapper::AppendNonce(array_view<uint8_t> nonce, uint8_t target[], size_t target_size, EncryptionMode mode)
	{
		switch (mode)
		{
		case XSalsa20_Poly1305_Lite:
			std::copy(nonce.begin(), &nonce.at(4), &target[target_size - 4]);
			break;
		case XSalsa20_Poly1305_Suffix:
			std::copy(nonce.begin(), nonce.end(), &target[target_size - 12]);
			break;
		}
	}

	SodiumWrapper::~SodiumWrapper()
	{
		delete[] key;
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

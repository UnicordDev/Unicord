// Parts of the code adapted from:
// https://github.com/adamcaudill/libsodium-net/blob/master/libsodium-net/SecretBox.cs
// https://github.com/adamcaudill/libsodium-net/blob/master/libsodium-net/SodiumLibrary.cs

using Sodium;
using System;
using System.Runtime.InteropServices;
#if !NETSTANDARD1_1
using System.Security.Cryptography;
#endif

namespace DSharpPlus.VoiceNext.Codec
{
    internal sealed class SodiumCodec
    {
        private const int KEY_BYTES = 32;
        private const int NONCE_BYTES = 24;
        private const int MAC_BYTES = 16;

        public byte[] Encode(byte[] input, byte[] nonce, byte[] secretKey)
        {
            if (secretKey == null || secretKey.Length != KEY_BYTES)
                throw new ArgumentException("Invalid key.");

            if (nonce == null || nonce.Length != NONCE_BYTES)
                throw new ArgumentException("Invalid nonce.");           

            return SecretBox.Create(input, nonce, secretKey);
        }

        public byte[] Decode(byte[] input, byte[] nonce, byte[] secretKey)
        {
            if (secretKey == null || secretKey.Length != KEY_BYTES)
                throw new ArgumentException("Invalid key.");

            if (nonce == null || nonce.Length != NONCE_BYTES)
                throw new ArgumentException("Invalid nonce.");

            return SecretBox.Open(input, nonce, secretKey);
        }
    }
}

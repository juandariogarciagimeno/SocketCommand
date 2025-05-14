using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using System.Security.Cryptography;
using System.Text;

namespace SocketCommand.Hosting.Defaults
{
    public class DefaultSocketMessageAESEncryption : ISocketMessasgeEncryption
    {
        private static string DefaultKey = "oD6keRx6oCOgpN6hKJ06+CPtbBrySYBfs3ll/fbm8wg=";
        private static string DefaultIV = "pNLMzlkfP0nJvxDylJSOQA==";

        private SocketConfiguration config;

        public DefaultSocketMessageAESEncryption(IOptions<SocketConfiguration> config)
        {
            this.config = config.Value;
        }

        public Task<byte[]> Decrypt(byte[] data)
        {
            using var aes = OpenAESKey();
            return Task.FromResult(aes.DecryptCbc(data, aes.IV));
        }

        public Task<byte[]> Encrypt(byte[] data)
        {
            using var aes = OpenAESKey();
            return Task.FromResult(aes.EncryptCbc(data, aes.IV));
        }

        private Aes OpenAESKey()
        {
            var aesAlg = Aes.Create();

            byte[] key = Convert.FromBase64String(string.IsNullOrEmpty(config?.AESKey) ? DefaultKey : config.AESKey);
            byte[] iv = Convert.FromBase64String(string.IsNullOrEmpty(config?.AESIV) ? DefaultIV : config.AESIV);

            if (!aesAlg.ValidKeySize((int)(key.Length * 8L)))
                throw new ArgumentException($"Invalid AES key size: {key.Length * 8L} bits. Valid sizes are: {string.Join(", ", aesAlg.LegalKeySizes.Select(x => x.MaxSize))}");

            if (iv.Length != aesAlg.BlockSize / 8)
                throw new ArgumentException($"Invalid AES IV size: {iv.Length} bytes. Valid size is: {aesAlg.BlockSize / 8} bytes.");

            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Key = key;
            aesAlg.IV = iv;

            return aesAlg;
        }
    }
}

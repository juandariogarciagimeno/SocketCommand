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
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Key = Convert.FromBase64String(string.IsNullOrEmpty(config.AESKey) ? DefaultKey : config.AESKey);
            aesAlg.IV = Convert.FromBase64String(string.IsNullOrEmpty(config.AESIV) ? DefaultIV : config.AESIV);

            return aesAlg;
        }
    }
}

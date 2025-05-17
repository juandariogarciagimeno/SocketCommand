// --------------------------------------------------------------------------------------------------
// <copyright file="DefaultSocketMessageAESEncryption.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Defaults;

using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting.Config;
using System.Security.Cryptography;
public class DefaultSocketMessageAESEncryption : ISocketMessageEncryption
{
    private const string DefaultKey = "oD6keRx6oCOgpN6hKJ06+CPtbBrySYBfs3ll/fbm8wg=";
    private const string DefaultIV = "pNLMzlkfP0nJvxDylJSOQA==";

    private SocketConfiguration config;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSocketMessageAESEncryption"/> class.
    /// </summary>
    /// <param name="config">Socket Configuration.</param>
    public DefaultSocketMessageAESEncryption(IOptions<SocketConfiguration> config)
    {
        this.config = config.Value;
    }

    /// <summary>
    /// Decrytps the data using the configured AES Key and IV.
    /// </summary>
    /// <param name="data">Encrypted data.</param>
    /// <returns>Decrypted data.</returns>
    public Task<byte[]> Decrypt(byte[] data)
    {
        using var aes = OpenAESKey();
        return Task.FromResult(aes.DecryptCbc(data, aes.IV));
    }

    /// <summary>
    /// Encrypts the data using the configured AES Key and IV.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <returns>Encrypted Data.</returns>
    public Task<byte[]> Encrypt(byte[] data)
    {
        using var aes = OpenAESKey();
        return Task.FromResult(aes.EncryptCbc(data, aes.IV));
    }

    /// <summary>
    /// Opens the AES key and IV from the configuration or uses the default values.
    /// </summary>
    /// <returns>The Initialized AES Key.</returns>
    /// <exception cref="ArgumentException">If key or IV are invalid.</exception>
    private Aes OpenAESKey()
    {
        var aesAlg = Aes.Create();

        byte[] key = Convert.FromBase64String(string.IsNullOrEmpty(config?.AESKey) ? DefaultKey : config.AESKey);
        byte[] iv = Convert.FromBase64String(string.IsNullOrEmpty(config?.AESIV) ? DefaultIV : config.AESIV);

        if (!aesAlg.ValidKeySize((int)(key.Length * 8L)))
        {
            throw new ArgumentException($"Invalid AES key size: {key.Length * 8L} bits. Valid sizes are: {string.Join(", ", aesAlg.LegalKeySizes.Select(x => x.MaxSize))}");
        }

        if (iv.Length != aesAlg.BlockSize / 8)
        {
            throw new ArgumentException($"Invalid AES IV size: {iv.Length} bytes. Valid size is: {aesAlg.BlockSize / 8} bytes.");
        }

        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Key = key;
        aesAlg.IV = iv;

        return aesAlg;
    }
}

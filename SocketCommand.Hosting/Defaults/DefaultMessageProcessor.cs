// --------------------------------------------------------------------------------------------------
// <copyright file="DefaultMessageProcessor.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Defaults;

using Microsoft.Extensions.DependencyInjection;
using SocketCommand.Abstractions.Interfaces;

/// <summary>
/// Default message processor. Handles common message operations such as encryption and compression.
/// </summary>
internal class DefaultMessageProcessor
{
    private ISocketMessageSerializer serializer;
    private ISocketMessageCompressor? compressor;
    private ISocketMessageEncryption? encryptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessageProcessor"/> class.
    /// </summary>
    /// <param name="sp">Service Provider.</param>
    public DefaultMessageProcessor(IServiceProvider sp)
    {
        serializer = sp.GetRequiredService<ISocketMessageSerializer>();
        compressor = sp.GetService<ISocketMessageCompressor>();
        encryptor = sp.GetService<ISocketMessageEncryption>();
    }

    /// <summary>
    /// Compresses and encrypts the data.
    /// </summary>
    /// <param name="data">Input data.</param>
    /// <returns>The data compressed and serialized.</returns>
    internal async Task<byte[]> CompressAndEncrypt(byte[] data)
    {
        try
        {
            if (compressor != null)
            {
                data = compressor.Compress(data);
            }

            if (encryptor != null)
            {
                data = await encryptor.Encrypt(data);
            }

            return data;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Serializes the object to a byte array, applying encryption and compression if applicable.
    /// </summary>
    /// <param name="o">Object to serialize.</param>
    /// <returns>The serialized object.</returns>
    internal async Task<byte[]> Serialize(object o)
    {
        try
        {
            var data = serializer.Serialize(o);
            if (compressor != null)
            {
                data = compressor.Compress(data);
            }

            if (encryptor != null)
            {
                data = await encryptor.Encrypt(data);
            }

            return data;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Deserializes the byte array to an object, applying decryption and decompression if applicable.
    /// </summary>
    /// <param name="data">Data to deserialize.</param>
    /// <param name="type">Type to deserialize into.</param>
    /// <returns>The deserialized object.</returns>
    internal async Task<object?> Deserialize(byte[] data, Type type)
    {
        try
        {
            if (encryptor != null)
            {
                data = await encryptor.Decrypt(data);
            }

            if (compressor != null)
            {
                data = compressor.Decompress(data);
            }

            var message = serializer.Deserialize(data, type);
            return message;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes the byte array to an object of type T, applying decryption and decompression if applicable.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to.</typeparam>
    /// <param name="data">Data to deserialize.</param>
    /// <returns>Deserialized object.</returns>
    internal async Task<T?> Deserialize<T>(byte[] data)
    {
        var message = await Deserialize(data, typeof(T));
        if (message is T t)
        {
            return t;
        }
        else
        {
            return default;
        }
    }

    /// <summary>
    /// Applies decryption and decompression to the data if applicable.
    /// </summary>
    /// <param name="data">Data to process.</param>
    /// <returns>Processed data.</returns>
    internal async Task<byte[]> DecryptAndDecompress(byte[] data)
    {
        try
        {
            if (encryptor != null)
            {
                data = await encryptor.Decrypt(data);
            }

            if (compressor != null)
            {
                data = compressor.Decompress(data);
            }

            return data;
        }
        catch
        {
            return [];
        }
    }
}

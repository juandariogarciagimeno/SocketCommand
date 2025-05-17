// --------------------------------------------------------------------------------------------------
// <copyright file="ISocketMessageEncryption.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Interfaces;

/// <summary>
/// Interface for socket message encryption.
/// </summary>
public interface ISocketMessageEncryption
{
    /// <summary>
    /// Encrypts data.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <returns>Encrypted data.</returns>
    Task<byte[]> Encrypt(byte[] data);

    /// <summary>
    /// Decrypts data.
    /// </summary>
    /// <param name="data">Encrypted data.</param>
    /// <returns>Decrypted data.</returns>
    Task<byte[]> Decrypt(byte[] data);
}

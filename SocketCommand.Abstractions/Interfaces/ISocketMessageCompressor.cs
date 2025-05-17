// --------------------------------------------------------------------------------------------------
// <copyright file="ISocketMessageCompressor.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Interfaces;

/// <summary>
/// Interface for socket message compression.
/// </summary>
public interface ISocketMessageCompressor
{
    /// <summary>
    /// Compresses data.
    /// </summary>
    /// <param name="data">Data to compress.</param>
    /// <returns>The compressed data.</returns>
    public byte[] Compress(byte[] data);

    /// <summary>
    /// Decompresses data.
    /// </summary>
    /// <param name="data">Compressed  data.</param>
    /// <returns>Decompressed data.</returns>
    public byte[] Decompress(byte[] data);
}

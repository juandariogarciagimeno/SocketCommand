// --------------------------------------------------------------------------------------------------
// <copyright file="DefaultSocketMessageCompressor.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Compression._7Zip;

using SocketCommand.Abstractions.Interfaces;

/// <summary>
/// Default implementation for socket message compression using 7ZIP and LZMA.
/// </summary>
public class SevenZipMessageCompressor : ISocketMessageCompressor
{
    /// <summary>
    /// Compresses the given byte array using LZMA compression.
    /// </summary>
    /// <param name="data">Data to compress.</param>
    /// <returns>The compressed data.</returns>
    public byte[] Compress(byte[] data)
    {
        return SevenZipCompressor.CompressLZMA(data);
    }

    /// <summary>
    /// Decompresses the given byte array using LZMA decompression.
    /// </summary>
    /// <param name="data">Data to decompress.</param>
    /// <returns>Decompressed data,</returns>
    public byte[] Decompress(byte[] data)
    {
        return SevenZipCompressor.DecompressLZMA(data);
    }
}

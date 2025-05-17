// --------------------------------------------------------------------------------------------------
// <copyright file="ISocketMessageSerializer.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Interfaces;

/// <summary>
/// Interface for socket message serialization.
/// </summary>
public interface ISocketMessageSerializer
{
    /// <summary>
    /// Serializes an object to a byte array.
    /// </summary>
    /// <param name="data">Object to serialize.</param>
    /// <returns>The serialized data.</returns>
    public byte[] Serialize(object data);

    /// <summary>
    /// Deserializes a byte array to an object of the specified type.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <param name="type">Target type.</param>
    /// <returns>The deserialized object.</returns>
    public object? Deserialize(byte[] data, Type type);
}

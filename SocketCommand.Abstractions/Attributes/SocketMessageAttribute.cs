// --------------------------------------------------------------------------------------------------
// <copyright file="SocketMessageAttribute.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Attributes;

/// <summary>
/// Attribute to specify the class is a serializable to socket message.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SocketMessageAttribute() : Attribute
{
}

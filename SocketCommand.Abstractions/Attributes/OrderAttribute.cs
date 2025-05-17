// --------------------------------------------------------------------------------------------------
// <copyright file="OrderAttribute.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Attributes;

/// <summary>
/// Attribute to specify the order of properties.
/// </summary>
/// <param name="order">Order of the property.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class OrderAttribute(int order) : Attribute
{
    public int Order { get; set; } = order;
}

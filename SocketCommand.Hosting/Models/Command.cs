// --------------------------------------------------------------------------------------------------
// <copyright file="Command.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Models;

/// <summary>
/// Command model for Socket data.
/// </summary>
internal sealed class Command
{
    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate to handle the command.
    /// </summary>
    public Delegate Handler { get; set; } = null!;

    /// <summary>
    /// Gets or sets the caster for the handler type.
    /// </summary>
    public Func<object, object>? Caster { get; set; }
}

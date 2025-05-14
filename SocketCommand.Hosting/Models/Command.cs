namespace SocketCommand.Hosting.Models;

internal sealed class Command
{
    public string Name { get; set; }

    public Delegate Handler { get; set; }

    public Func<object, object>? Caster { get; set; }
}

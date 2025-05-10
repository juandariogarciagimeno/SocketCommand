namespace SocketCommand.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class OrderAttribute(int order) : Attribute
{
    public int Order { get; set; } = order;
}

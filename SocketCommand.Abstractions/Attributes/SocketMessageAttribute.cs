namespace SocketCommand.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class SocketMessageAttribute(bool compress = false) : Attribute
    {
        public bool Compress { get; set; } = compress;
    }
}

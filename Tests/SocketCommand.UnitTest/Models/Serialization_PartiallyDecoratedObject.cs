namespace SocketCommand.UnitTest.Models;

using SocketCommand.Abstractions.Attributes;
using Order = Abstractions.Attributes.OrderAttribute;


[SocketMessage]
public class PartiallyDecoratedObject
{
    [@Order(1)]
    public int IntValue { get; set; }

    [@Order(2)]
    public long LongValue { get; set; }

    public short ShortValue { get; set; }

    public byte ByteValue { get; set; }

    [@Order(3)]
    public bool BoolValue { get; set; }

    public float FloatValue { get; set; }

    public double DoubleValue { get; set; }

    [@Order(8)]
    public string StringValue { get; set; }

    public static PartiallyDecoratedObject Example = new PartiallyDecoratedObject
    {
        IntValue = 42,
        LongValue = 1234567890L,
        ShortValue = 32000,
        ByteValue = 255,
        BoolValue = true,
        FloatValue = 3.14f,
        DoubleValue = 6.28,
        StringValue = "Hello"
    };

    public override bool Equals(object obj)
    {
        if (obj is not PartiallyDecoratedObject other)
            return false;

        return IntValue == other.IntValue &&
               LongValue == other.LongValue &&
               BoolValue == other.BoolValue &&
               StringValue == other.StringValue;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IntValue, LongValue, BoolValue, StringValue);
    }
}
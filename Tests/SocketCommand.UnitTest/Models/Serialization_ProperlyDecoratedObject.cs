namespace SocketCommand.UnitTest.Models;

using SocketCommand.Abstractions.Attributes;
using Order = Abstractions.Attributes.OrderAttribute;

public enum TestStatus
{
    Unknown = 0,
    Started = 1,
    Completed = 2,
    Failed = 3
}

[SocketMessage]
public class TestNestedObject
{
    [@Order(1)]
    public string Description { get; set; }

    [@Order(2)]
    public bool IsValid { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not TestNestedObject other)
            return false;

        return Description == other.Description &&
               IsValid == other.IsValid;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Description, IsValid);
    }
}

[SocketMessage]
public class DecoratedObject
{
    [@Order(1)]
    public int IntValue { get; set; }

    [@Order(2)]
    public long LongValue { get; set; }

    [@Order(3)]
    public short ShortValue { get; set; }

    [@Order(4)]
    public byte ByteValue { get; set; }

    [@Order(5)]
    public bool BoolValue { get; set; }

    [@Order(6)]
    public float FloatValue { get; set; }

    [@Order(7)]
    public double DoubleValue { get; set; }

    [@Order(8)]
    public string StringValue { get; set; }

    [@Order(9)]
    public TestStatus Status { get; set; }

    [@Order(10)]
    public TestNestedObject Nested { get; set; }

    public static DecoratedObject Example = new DecoratedObject
    {
        IntValue = 42,
        LongValue = 1234567890L,
        ShortValue = 32000,
        ByteValue = 255,
        BoolValue = true,
        FloatValue = 3.14f,
        DoubleValue = 6.28,
        StringValue = "Hello",
        Status = TestStatus.Completed,
        Nested = new TestNestedObject
        {
            Description = "Nested Object",
            IsValid = true
        }
    };

    public override bool Equals(object obj)
    {
        if (obj is not DecoratedObject other)
            return false;

        return IntValue == other.IntValue &&
               LongValue == other.LongValue &&
               ShortValue == other.ShortValue &&
               ByteValue == other.ByteValue &&
               BoolValue == other.BoolValue &&
               FloatValue.Equals(other.FloatValue) &&
               DoubleValue.Equals(other.DoubleValue) &&
               StringValue == other.StringValue &&
               Status == other.Status &&
               (Nested?.Equals(other.Nested) ?? other.Nested is null);
    }

    public override int GetHashCode()
    {
        var hash1 = HashCode.Combine(IntValue, LongValue, ShortValue, ByteValue,
                                     BoolValue, FloatValue, DoubleValue, StringValue);

        return HashCode.Combine(hash1, Status, Nested);
    }
}
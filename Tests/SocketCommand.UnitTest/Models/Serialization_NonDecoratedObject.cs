namespace SocketCommand.UnitTest.Models;

public class NonDecoratedObject
{
    public int IntValue { get; set; }

    public string StringValue { get; set; }


    public static NonDecoratedObject Example = new NonDecoratedObject
    {
        IntValue = 42,
        StringValue = "Hello"
    };
}
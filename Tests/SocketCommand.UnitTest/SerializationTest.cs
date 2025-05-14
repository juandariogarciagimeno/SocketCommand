using SocketCommand.Hosting.Defaults;
using SocketCommand.UnitTest.Models;

namespace SocketCommand.UnitTest;

[TestFixture]
public class SerializationTest
{
    [Test]
    [TestCase("KtKF2MwEAH3/AcP1SEAfhetRuB4ZQAVIZWxsbwIAAAANTmVzdGVkIE9iamVjdAE=")]
    public void Serialize_default_serializer_with_properly_decorated_object_with_all_supported_datatypes(string expected)
    {
        var serializer = new DefaultSocketMessageSerializer();
        var serialized = Convert.ToBase64String(serializer.Serialize(DecoratedObject.Example));
        Assert.That(serialized, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("KtKF2MwEAH3/AcP1SEAfhetRuB4ZQAVIZWxsbwIAAAANTmVzdGVkIE9iamVjdAE=")]
    public void Deserialize_default_serializer_with_properly_decorated_object_with_all_supported_datatypes(string serialized)
    {
        var serializer = new DefaultSocketMessageSerializer();
        var deserialized = serializer.Deserialize(Convert.FromBase64String(serialized), typeof(DecoratedObject));
        Assert.That(deserialized, Is.EqualTo(DecoratedObject.Example));
    }

    [Test]
    public void Serialize_default_serializer_with_non_decorated_object_with_all_supported_datatypes()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var serializer = new DefaultSocketMessageSerializer();
            var serialized = Convert.ToBase64String(serializer.Serialize(NonDecoratedObject.Example));
        });
    }

    [Test]
    [TestCase("KtKF2MwEAQVIZWxsbw==")]
    public void Serialize_default_serializer_with_partially_decorated_object_only_ordered_should_serialize(string expected)
    {
        var serializer = new DefaultSocketMessageSerializer();
        var serialized = Convert.ToBase64String(serializer.Serialize(PartiallyDecoratedObject.Example));
        Assert.That(serialized, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("KtKF2MwEAQVIZWxsbw==")]
    public void Deserialize_default_serializer_with_partially_decorated_object_only_ordered_should_deserialize(string serialized)
    {
        var serializer = new DefaultSocketMessageSerializer();
        var deserialized = serializer.Deserialize(Convert.FromBase64String(serialized), typeof(PartiallyDecoratedObject));
        Assert.That(deserialized.Equals(PartiallyDecoratedObject.Example));
    }

}


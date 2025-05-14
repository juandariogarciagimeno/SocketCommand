namespace SocketCommand.UnitTest;

using System.Text;
using SocketCommand.Hosting.Defaults;

[TestFixture]
public class CompressionTest
{
    [Test]
    [TestCase("Hello World", "XQAAIAALAAAAAAAAAAAkGUmYbxARyF/m1YnYfQA=")]
    [TestCase("Hello World Hello World Hello World Hello World Hello World Hello World Hello World Hello World", "XQAAIABfAAAAAAAAAAAkGUmYbxARyF/m1Ypi2jgAXgAA")]
    public void Compress_default_compressor_valid_data(string data, string expected)
    {
        var compressor = new DefaultSocketMessageCompressor();
        var compressed = Convert.ToBase64String(compressor.Compress(Encoding.UTF8.GetBytes(data)));
        Assert.That(expected, Is.EqualTo(compressed));
    }

    [Test]
    [TestCase("XQAAIAALAAAAAAAAAAAkGUmYbxARyF/m1YnYfQA=", "Hello World")]
    [TestCase("XQAAIABfAAAAAAAAAAAkGUmYbxARyF/m1Ypi2jgAXgAA", "Hello World Hello World Hello World Hello World Hello World Hello World Hello World Hello World")]
    public void Decompress_default_compressor_valid_data(string data, string expected)
    {
        var compressor = new DefaultSocketMessageCompressor();
        var compressed = Encoding.UTF8.GetString(compressor.Decompress(Convert.FromBase64String(data)));
        Assert.That(expected, Is.EqualTo(compressed));
    }

    [Test]
    [TestCase("SGVsbG8gV29ybGQ=")]
    [TestCase("SGVsbG8gV29ybGRIZWxsbyBXb3JsZEhlbGxvIFdvcmxk")]
    public void Decompress_default_compressor_invalid_data(string data)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var compressor = new DefaultSocketMessageCompressor();
            var compressed = Encoding.UTF8.GetString(compressor.Decompress(Convert.FromBase64String(data)));
        });
    }

    [Test]
    public void Compress_default_compressor_zero_data()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var compressor = new DefaultSocketMessageCompressor();
            var compressed = Convert.ToBase64String(compressor.Compress([]));
        });
    }
}

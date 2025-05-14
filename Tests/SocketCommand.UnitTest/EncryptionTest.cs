using Microsoft.Extensions.Options;
using Moq;
using SocketCommand.Core.Config;
using SocketCommand.Hosting.Defaults;
using System.Text;

namespace SocketCommand.UnitTest
{

    [TestFixture]
    public class EncryptionTest
    {
        private static SocketConfiguration _validConfigData = new SocketConfiguration()
        {
            AESKey = "jiwlQEjVDC67rIbte8n0XoZZzgXE4cSnRlj81YZaAf4=",
            AESIV = "ivgVH9nmCwAafSK2jhyA1Q=="
        };

        private static SocketConfiguration _invalidConfigData = new SocketConfiguration()
        {
            AESKey = "YmFka2V5",//badkey
            AESIV = "YmFkaXY=",//badiv
        };

        [Test]
        [TestCase("Hello World", "Je1kTx/QUWMQdcESJP513g==")]
        public async Task Encrypt_default_encryptor_valid_custom_key(string data, string expected)
        {
            var configMock = new Mock<IOptions<SocketConfiguration>>();
            configMock.Setup(x => x.Value).Returns(_validConfigData);

            var encryptor = new DefaultSocketMessageAESEncryption(configMock.Object);
            var encrypted = Convert.ToBase64String(await encryptor.Encrypt(Encoding.UTF8.GetBytes(data)));

            Assert.That(expected, Is.EqualTo(encrypted));
        }

        [Test]
        [TestCase("Je1kTx/QUWMQdcESJP513g==", "Hello World")]
        public async Task Decrypt_default_encryptor_valid_custom_key(string data, string expected)
        {
            var configMock = new Mock<IOptions<SocketConfiguration>>();
            configMock.Setup(x => x.Value).Returns(_validConfigData);

            var encryptor = new DefaultSocketMessageAESEncryption(configMock.Object);
            var decrypted = Encoding.UTF8.GetString(await encryptor.Decrypt(Convert.FromBase64String(data)));

            Assert.That(expected, Is.EqualTo(decrypted));
        }

        [Test]
        [TestCase("Hello World", "6Q/EknSnMEJ5/BEDgT3NfA==")]
        public async Task Encrypt_default_encryptor_default_key(string data, string expected)
        {
            var configMock = new Mock<IOptions<SocketConfiguration>>();
            configMock.Setup(x => x.Value).Returns<SocketConfiguration>(null);

            var encryptor = new DefaultSocketMessageAESEncryption(configMock.Object);
            var encrypted = Convert.ToBase64String(await encryptor.Encrypt(Encoding.UTF8.GetBytes(data)));

            Assert.That(expected, Is.EqualTo(encrypted));
        }

        [Test]
        [TestCase("6Q/EknSnMEJ5/BEDgT3NfA==", "Hello World")]
        public async Task Decrypt_default_encryptor_default_key(string data, string expected)
        {
            var configMock = new Mock<IOptions<SocketConfiguration>>();
            configMock.Setup(x => x.Value).Returns<SocketConfiguration>(null);

            var encryptor = new DefaultSocketMessageAESEncryption(configMock.Object);
            var decrypted = Encoding.UTF8.GetString(await encryptor.Decrypt(Convert.FromBase64String(data)));

            Assert.That(expected, Is.EqualTo(decrypted));
        }

        [Test]
        [TestCase("Hello World")]
        public void Encrypt_default_encryptor_invalid_key(string data)
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var configMock = new Mock<IOptions<SocketConfiguration>>();
                configMock.Setup(x => x.Value).Returns(_invalidConfigData);

                var encryptor = new DefaultSocketMessageAESEncryption(configMock.Object);
                var encrypted = Convert.ToBase64String(await encryptor.Encrypt(Encoding.UTF8.GetBytes(data)));
            });
        }

        [Test]
        [TestCase("6Q/EknSnMEJ5/BEDgT3NfA==")]
        public void Decrypt_default_encryptor_invalid_key(string data)
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var configMock = new Mock<IOptions<SocketConfiguration>>();
                configMock.Setup(x => x.Value).Returns(_invalidConfigData);

                var encryptor = new DefaultSocketMessageAESEncryption(configMock.Object);
                var decrypted = Encoding.UTF8.GetString(await encryptor.Decrypt(Convert.FromBase64String(data)));
            });

        }
    }
}
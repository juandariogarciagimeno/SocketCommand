using Microsoft.Extensions.DependencyInjection;
using SocketCommand.Abstractions.Interfaces;

namespace SocketCommand.Hosting.Defaults
{
    internal class DefaultMessageProcessor
    {

        private ISocketMessageSerializer serializer;
        private ISocketMessageCompressor? compressor;
        private ISocketMessageEncryption? encryptor;

        public DefaultMessageProcessor(IServiceProvider sp)
        {
            this.serializer = sp.GetRequiredService<ISocketMessageSerializer>();
            this.compressor = sp.GetService<ISocketMessageCompressor>();
            this.encryptor = sp.GetService<ISocketMessageEncryption>();
        }

        internal async Task<byte[]> CompressAndEncrypt(byte[] data)
        {
            try
            {
                if (compressor != null)
                {
                    data = compressor.Compress(data);
                }

                if (encryptor != null)
                {
                    data = await encryptor.Encrypt(data);
                }

                return data;
            }
            catch { return []; }
        }

        internal async Task<byte[]> Serialize(object o)
        {
            try
            {
                var data = serializer.Serialize(o);
                if (compressor != null)
                {
                    data = compressor.Compress(data);
                }

                if (encryptor != null)
                {
                    data = await encryptor.Encrypt(data);
                }

                return data;
            }
            catch { return []; }
        }

        internal async Task<object?> Deserialize(byte[] data, Type type)
        {
            try
            {
                if (encryptor != null)
                {
                    data = await encryptor.Decrypt(data);
                }

                if (compressor != null)
                {
                    data = compressor.Decompress(data);
                }

                var message = serializer.Deserialize(data, type);
                return message;
            }
            catch { return default; }
        }

        internal async Task<T?> Deserialize<T>(byte[] data)
        {
            var message = await Deserialize(data, typeof(T));
            if (message is T t)
            {
                return t;
            }
            else
            {
                return default;
            }
        }

        internal async Task<byte[]> DecryptAndDecompress(byte[] data)
        {
            try
            {
                if (encryptor != null)
                {
                    data = await encryptor.Decrypt(data);
                }

                if (compressor != null)
                {
                    data = compressor.Decompress(data);
                }

                return data;
            }
            catch { return []; }
        }
    }
}

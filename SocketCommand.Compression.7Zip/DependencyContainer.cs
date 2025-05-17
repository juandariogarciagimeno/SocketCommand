using SocketCommand.Abstractions.Interfaces;

namespace SocketCommand.Compression._7Zip
{
    public static class DependencyContainer
    {
        /// <summary>
        /// Applies the 7Zip compression provider to the socket communication system.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ISocketHostBuilder With7ZipCompression(this ISocketHostBuilder builder)
        {
            builder.WithCompression<SevenZipMessageCompressor>();
            return builder;
        } 
    }
}

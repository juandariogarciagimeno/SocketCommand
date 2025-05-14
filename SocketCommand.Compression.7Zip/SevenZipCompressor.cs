using SevenZip;

namespace SocketCommand.Compression._7Zip
{
    public static class SevenZipCompressor
    {
        public static byte[] CompressLZMA(byte[] data)
        {
            if (data.Length == 0)
                throw new ArgumentException("Data is empty.");

            using MemoryStream inputStream = new MemoryStream(data);
            using MemoryStream outputStream = new MemoryStream();

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();

            var props = GetEncoderProperties();
            encoder.SetCoderProperties(props.Item1, props.Item2);

            encoder.WriteCoderProperties(outputStream);

            // Write the decompressed file size.
            outputStream.Write(BitConverter.GetBytes(inputStream.Length), 0, 8);

            // Encode the file.
            encoder.Code(inputStream, outputStream, inputStream.Length, -1, null);

            return outputStream.ToArray();
        }
        public static byte[] DecompressLZMA(byte[] data)
        {
            try
            {
                if (data.Length < 13)
                    throw new ArgumentException("Data is too short to be a valid LZMA compressed stream.");

                using MemoryStream inputStream = new MemoryStream(data);
                using MemoryStream outputStream = new MemoryStream();

                SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

                byte[] properties = new byte[5];
                inputStream.Read(properties, 0, 5);

                byte[] fileLengthBytes = new byte[8];
                inputStream.Read(fileLengthBytes, 0, 8);
                long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

                decoder.SetDecoderProperties(properties);
                decoder.Code(inputStream, outputStream, inputStream.Length, fileLength, null);

                return outputStream.ToArray();
            }
            catch (DataErrorException)
            {
                throw new ArgumentException("Data has invalid format. Probably wasn't compressed");
            }
        }

        private static (CoderPropID[], object[] prop) GetEncoderProperties()
        {
            bool eos = false;
            Int32 dictionary = 1 << 21;
            Int32 posStateBits = 2;
            Int32 litContextBits = 3; // for normal files
                                      // UInt32 litContextBits = 0; // for 32-bit data
            Int32 litPosBits = 0;
            // UInt32 litPosBits = 2; // for 32-bit data
            Int32 algorithm = 2;
            Int32 numFastBytes = 128;
            string mf = "bt4";

           CoderPropID[] propIDs = new CoderPropID[]
           {
               CoderPropID.DictionarySize,
               CoderPropID.PosStateBits,
               CoderPropID.LitContextBits,
               CoderPropID.LitPosBits,
               CoderPropID.Algorithm,
               CoderPropID.NumFastBytes,
               CoderPropID.MatchFinder,
               CoderPropID.EndMarker
           };
           object[] properties = new object[]
           {
               dictionary,
               posStateBits,
               litContextBits,
               litPosBits,
               algorithm,
               numFastBytes,
               mf,
               eos
            };

            return (propIDs, properties);
        }
    }
}

using System;
using System.IO;
using System.Text;

namespace NuGet {
    public static class StreamExtensions {
        public static byte[] ReadAllBytes(this Stream stream) {
            int length = (int)stream.Length;
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return buffer;
        }

        /// <summary>
        /// Turns an existing stream into one that a stream factory that can be reopened.
        /// </summary>        
        public static Func<Stream> ToStreamFactory(this Stream stream) {
            byte[] buffer;

            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);
                buffer = ms.ToArray();
            }

            return () => new MemoryStream(buffer);
        }

        public static string ReadToEnd(this Stream stream) {
            using (var streamReader = new StreamReader(stream)) {
                return streamReader.ReadToEnd();
            }
        }

        public static Stream AsStream(this string value) {
            return AsStream(value, Encoding.Default);
        }

        public static Stream AsStream(this string value, Encoding encoding) {
            return new MemoryStream(encoding.GetBytes(value));
        }

        public static bool ContentEquals(this Stream stream, Stream otherStream) {
            return Crc32.Calculate(stream) == Crc32.Calculate(otherStream);
        }
    }
}

using System.IO;
using System.Text;

namespace NuGet {
    public static class StreamExtensions {
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

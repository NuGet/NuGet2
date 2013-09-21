using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NuGet
{
    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this Stream stream)
        {
            var memoryStream = stream as MemoryStream;
            if (memoryStream != null)
            {
                return memoryStream.ToArray();
            }
            else
            {
                using (memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Turns an existing stream into one that a stream factory that can be reopened.
        /// </summary>        
        public static Func<Stream> ToStreamFactory(this Stream stream)
        {
            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                try
                {
                    stream.CopyTo(ms);
                    buffer = ms.ToArray();
                }
                finally 
                {
                    stream.Close();
                }
            }

            return () => new MemoryStream(buffer);
        }

        public static string ReadToEnd(this Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static Stream AsStream(this string value)
        {
            return AsStream(value, Encoding.UTF8);
        }

        public static Stream AsStream(this string value, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(value));
        }

        public static bool ContentEquals(this Stream stream, Stream otherStream)
        {
            if (stream.CanSeek && otherStream.CanSeek)
            {
                if (stream.Length != otherStream.Length)
                {
                    return false;
                }
            }

            bool isBinaryFile = IsBinary(otherStream);
            otherStream.Seek(0, SeekOrigin.Begin);

            return isBinaryFile ? CompareBinary(stream, otherStream) : CompareText(stream, otherStream);
        }

        public static bool IsBinary(Stream stream)
        {
            // quick and dirty hack to check if a stream represents binary content
            byte[] a = new byte[10];
            int bytesRead = stream.Read(a, 0, 10);
            int byteZeroIndex = Array.FindIndex(a, 0, bytesRead, d => d == 0);
            return byteZeroIndex >= 0;
        }

        private static bool CompareText(Stream stream, Stream otherStream)
        {
            
        }

        private static IEnumerable<string> ReadStreamLines(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                int counter = 0;
                while (reader.Peek() != -1)
                {
                    string line = reader.ReadLine();
                    if (line.EndsWith(Constants.BeginIgnoreMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        counter++;
                    }
                    else if (line.StartsWith(Constants.EndIgnoreMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        if (counter > 0)
                        {
                            counter--;
                        }
                    }
                    else
                    {
                        yield return line;
                    }
                }
            }
        }

        private static bool CompareBinary(Stream stream, Stream otherStream)
        {
            byte[] buffer = new byte[4 * 1024];
            byte[] otherBuffer = new byte[4 * 1024];

            int bytesRead = 0;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    int otherBytesRead = otherStream.Read(otherBuffer, 0, bytesRead);
                    if (bytesRead != otherBytesRead)
                    {
                        return false;
                    }

                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] != otherBuffer[i])
                        {
                            return false;
                        }
                    }
                }
            }
            while (bytesRead > 0);

            return true;
        }
    }
}

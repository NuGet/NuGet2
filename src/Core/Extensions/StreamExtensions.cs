using System;
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

            byte[] buffer = new byte[4*1024];
            byte[] otherBuffer = new byte[4*1024];

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

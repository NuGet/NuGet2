using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NuGet.ShimV3
{
    internal abstract class InterceptCallContext
    {
        public InterceptCallContext()
        {

        }

        public abstract InterceptorArguments Args { get;}

        public abstract Uri RequestUri { get; }
        public abstract string ResponseContentType { get; set; }
        public abstract Task WriteResponseAsync(byte[] data);

        public abstract void Log(string message, ConsoleColor color);

        public async Task WriteResponse(XElement feed)
        {
            ResponseContentType = "application/atom+xml; type=feed; charset=utf-8";

            MemoryStream stream = new MemoryStream();
            XmlWriter writer = XmlWriter.Create(stream);
            feed.WriteTo(writer);
            writer.Flush();
            byte[] data = stream.ToArray();

            await WriteResponseAsync(data);
        }

        public async Task WriteResponse(JToken jtoken)
        {
            ResponseContentType = "application/json; charset=utf-8";

            MemoryStream stream = new MemoryStream();
            TextWriter writer = new StreamWriter(stream);
            writer.Write(jtoken.ToString());
            writer.Flush();
            byte[] data = stream.ToArray();

            await WriteResponseAsync(data);
        }

        public async Task WriteResponse(string text)
        {
            ResponseContentType = "text/plain; charset=utf-8";

            MemoryStream stream = new MemoryStream();
            TextWriter writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(text);
            writer.Flush();
            byte[] data = stream.ToArray();

            await WriteResponseAsync(data);
        }
    }
}

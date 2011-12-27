using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NuGet
{
    internal static class XmlUtility
    {
        internal static XDocument GetOrCreateDocument(XName rootName, IFileSystem fileSystem, string path)
        {
            if (fileSystem.FileExists(path))
            {
                try
                {
                    return GetDocument(fileSystem, path);
                }
                catch (FileNotFoundException)
                {
                    return CreateDocument(rootName, fileSystem, path);
                }
            }
            return CreateDocument(rootName, fileSystem, path);
        }

        private static XDocument CreateDocument(XName rootName, IFileSystem fileSystem, string path)
        {
            XDocument document = new XDocument(new XElement(rootName));
            // Add it to the file system
            fileSystem.AddFile(path, document.Save);
            return document;
        }

        private static XDocument GetDocument(IFileSystem fileSystem, string path)
        {
            using (Stream configStream = fileSystem.OpenFile(path))
            {
                return XDocument.Load(configStream);
            }
        }

        internal static bool TryParseDocument(string content, out XDocument document)
        {
            document = null;
            try
            {
                document = XDocument.Parse(content);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}

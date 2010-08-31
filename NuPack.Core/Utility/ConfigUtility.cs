namespace NuPack {
    using System.IO;
    using System.Xml.Linq;

    internal static class ConfigUtility {
        internal static XDocument GetConfiguration(IFileSystem fileSystem, string path) {
            if (fileSystem.FileExists(path)) {
                try {
                    using (Stream configSream = fileSystem.OpenFile(path)) {
                        return XDocument.Load(configSream);
                    }
                }
                catch (FileNotFoundException) {
                    return CreateConfiguration(fileSystem, path);
                }
            }
            return CreateConfiguration(fileSystem, path);
        }

        private static XDocument CreateConfiguration(IFileSystem fileSystem, string path) {
            XDocument configuration = new XDocument(new XElement("configuration"));
            fileSystem.AddFile(path, configuration.Save);         
            return configuration;
        }
    }
}

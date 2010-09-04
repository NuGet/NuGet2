using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuPack {
    internal class XmlManifestWriter {
        private PackageBuilder _builder;
        public XmlManifestWriter(PackageBuilder builder) {
            _builder = builder;
        }

        public virtual void Save(Stream stream) {
            var document = new XDocument(new XElement("PackageSpec"));
            WriteMetaData(document.Root);
            WriteDependencies(document.Root);

            document.Save(stream);
        }
        
        private void WriteMetaData(XElement rootElement) {
            XElement metadataElement = new XElement("Metadata");
            if (!String.IsNullOrEmpty(_builder.Id)) {
                metadataElement.Add(new XElement("Identifier", _builder.Id));
            }
            if (_builder.Version != null) {
                metadataElement.Add(new XElement("Version", _builder.Version.ToString()));
            }
            if (!String.IsNullOrEmpty(_builder.Description)) {
                metadataElement.Add(new XElement("Description", _builder.Description));
            }
            if (_builder.Authors.Any()) {
                metadataElement.Add(new XElement("Authors",
                                    from author in _builder.Authors
                                    select new XElement("Author", author)));
            }

            if (!String.IsNullOrEmpty(_builder.Language)) {
                metadataElement.Add(new XElement("Language", _builder.Language));
            }

            metadataElement.Add(new XElement("Created", _builder.Created));
            metadataElement.Add(new XElement("Modified", _builder.Modified));

            if (!String.IsNullOrEmpty(_builder.LastModifiedBy)) {
                metadataElement.Add(new XElement("LastModifiedBy", _builder.LastModifiedBy));
            }
            if (!String.IsNullOrEmpty(_builder.Category)) {
                metadataElement.Add(new XElement("Category", _builder.Category));
            }
            if (_builder.Keywords.Any()) {
                metadataElement.Add(new XElement("Keywords", String.Join(", ", _builder.Keywords)));
            }

            rootElement.Add(metadataElement);
        }

        private void WriteDependencies(XElement rootElement) {
            if (_builder.Dependencies.Any()) {
                XElement dependenciesElement = new XElement("Dependencies");
                foreach (var dependency in _builder.Dependencies) {
                    var element = new XElement("Dependency", new XAttribute("id", dependency.Id));
                    if (dependency.Version != null) {
                        element.Add(new XAttribute("Version", dependency.Version));
                    }

                    if (dependency.MinVersion != null) {
                        element.Add(new XAttribute("MinVersion", dependency.MinVersion));
                    }

                    if (dependency.MaxVersion != null) {
                        element.Add(new XAttribute("MaxVersion", dependency.MaxVersion));
                    }
                    dependenciesElement.Add(element);
                }
                rootElement.Add(dependenciesElement);
            }
        }
    }
}

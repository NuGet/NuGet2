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
            var document = new XDocument(new XElement("package"));
            WriteMetaData(document.Root);
            WriteDependencies(document.Root);

            document.Save(stream);
        }

        private void WriteMetaData(XElement rootElement) {
            XElement metadataElement = new XElement("metadata",
                                           new XElement("id", _builder.Id),
                                           new XElement("version", _builder.Version.ToString()));
            
            if (!String.IsNullOrEmpty(_builder.Description)) {
                metadataElement.Add(new XElement("description", _builder.Description));
            }
            if (_builder.Authors.Any()) {
                metadataElement.Add(new XElement("authors",
                                    from author in _builder.Authors
                                    select new XElement("author", author)));
            }
            if (_builder.LicenseUrl != null) {
                metadataElement.Add(new XElement("licenseUrl", _builder.LicenseUrl));
            }
            if (!String.IsNullOrEmpty(_builder.Language)) {
                metadataElement.Add(new XElement("language", _builder.Language));
            }
            if (!String.IsNullOrEmpty(_builder.LastModifiedBy)) {
                metadataElement.Add(new XElement("lastmodifiedby", _builder.LastModifiedBy));
            }
            if (!String.IsNullOrEmpty(_builder.Category)) {
                metadataElement.Add(new XElement("category", _builder.Category));
            }
            if (_builder.Keywords.Any()) {
                metadataElement.Add(new XElement("keywords", String.Join(", ", _builder.Keywords)));
            }

            metadataElement.Add(new XElement("requireLicenseAcceptance", _builder.RequireLicenseAcceptance));
            metadataElement.Add(new XElement("created", _builder.Created));
            metadataElement.Add(new XElement("modified", _builder.Modified));

            rootElement.Add(metadataElement);
        }

        private void WriteDependencies(XElement rootElement) {
            if (_builder.Dependencies.Any()) {
                XElement dependenciesElement = new XElement("dependencies");
                foreach (var dependency in _builder.Dependencies) {
                    var element = new XElement("dependency", new XAttribute("id", dependency.Id));
                    if (dependency.Version != null) {
                        element.Add(new XAttribute("version", dependency.Version));
                    }

                    if (dependency.MinVersion != null) {
                        element.Add(new XAttribute("minversion", dependency.MinVersion));
                    }

                    if (dependency.MaxVersion != null) {
                        element.Add(new XAttribute("maxversion", dependency.MaxVersion));
                    }
                    dependenciesElement.Add(element);
                }
                rootElement.Add(dependenciesElement);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet {
    public class PackageReferenceFile {
        private readonly string _path;

        public PackageReferenceFile(string path) :
            this(new PhysicalFileSystem(Path.GetDirectoryName(path)),
                                        Path.GetFileName(path)) {
        }

        public PackageReferenceFile(IFileSystem fileSystem, string path) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            if (String.IsNullOrEmpty(path)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "path");
            }

            FileSystem = fileSystem;
            _path = path;
        }

        private IFileSystem FileSystem { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        public IEnumerable<PackageReference> GetPackageReferences() {
            XDocument document = GetDocument();

            if (document == null) {
                yield break;
            }

            foreach (var e in document.Root.Elements("package")) {
                string id = e.GetOptionalAttributeValue("id");
                string versionString = e.GetOptionalAttributeValue("version");
                Version version = VersionUtility.ParseOptionalVersion(versionString);

                yield return new PackageReference(id, version);
            }
        }

        /// <summary>
        /// Deletes an entry from the file with matching id and version. Returns true if the file was deleted.
        /// </summary>
        public bool DeleteEntry(string id, Version version) {
            XDocument document = GetDocument();

            if (document == null) {
                return false;
            }

            return DeleteEntry(document, id, version);
        }

        public bool EntryExists(string packageId, Version version) {
            XDocument document = GetDocument();
            if (document == null) {
                return false;
            }

            return FindEntry(document, packageId, version) != null;
        }

        public void AddEntry(string id, Version version) {
            XDocument document = GetDocument(createIfNotExists: true);

            AddEntry(document, id, version);
        }

        private void AddEntry(XDocument document, string id, Version version) {
            XElement element = FindEntry(document, id, version);

            if (element != null) {
                element.Remove();
            }

            document.Root.Add(new XElement("package",
                                  new XAttribute("id", id),
                                  new XAttribute("version", version)));

            SaveDocument(document);
        }

        private static XElement FindEntry(XDocument document, string id, Version version) {
            return (from e in document.Root.Elements("package")
                    let entryId = e.GetOptionalAttributeValue("id")
                    let entryVersion = VersionUtility.ParseOptionalVersion(e.GetOptionalAttributeValue("version"))
                    where entryId != null && entryVersion != null
                    where id.Equals(entryId, StringComparison.OrdinalIgnoreCase) &&
                          version.Equals(entryVersion)
                    select e).FirstOrDefault();
        }

        private void SaveDocument(XDocument document) {
            FileSystem.AddFile(_path, document.Save);
        }

        private bool DeleteEntry(XDocument document, string id, Version version) {
            XElement element = FindEntry(document, id, version);

            if (element != null) {
                // Remove the element from the xml dom
                element.Remove();

                // Remove the file if there are no more elements
                if (!document.Root.HasElements) {
                    FileSystem.DeleteFile(_path);

                    return true;
                }
                else {
                    // Otherwise save the updated document
                    SaveDocument(document);
                }
            }

            return false;
        }

        private XDocument GetDocument(bool createIfNotExists = false) {
            try {
                // If the file exists then open and return it
                if (FileSystem.FileExists(_path)) {
                    using (Stream stream = FileSystem.OpenFile(_path)) {
                        return XDocument.Load(stream);
                    }
                }

                // If it doesn't exist and we're creating a new file then return a
                // document with an empty packages node
                if (createIfNotExists) {
                    return new XDocument(new XElement("packages"));
                }

                return null;
            }
            catch (XmlException e) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.ErrorReadingFile, FileSystem.GetFullPath(_path)), e);
            }
        }
    }
}

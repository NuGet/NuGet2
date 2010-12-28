using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NuGet.Resources;

namespace NuGet {
    public class SharedPackageRepository : LocalPackageRepository, ISharedPackageRepository {
        private const string StoreFilePath = "repositories.config";

        public SharedPackageRepository(string path)
            : base(path) {
        }

        public SharedPackageRepository(IPackagePathResolver resolver, IFileSystem fileSystem)
            : base(resolver, fileSystem) {
        }

        public void RegisterRepository(string path) {
            AddEntry(path);
        }

        public void UnregisterRepository(string path) {
            DeleteEntry(path);
        }

        public bool IsReferenced(string packageId, Version version) {
            // See if this package exists in any other repository before we remove it
            return GetRepositories().Any(r => r.Exists(packageId, version));
        }

        protected virtual IPackageRepository CreateRepository(string path) {
            string root = PathUtility.EnsureTrailingSlash(FileSystem.Root);
            string absolutePath = PathUtility.GetAbsolutePath(root, path);
            string directory = Path.GetDirectoryName(absolutePath);
            return new PackageReferenceRepository(new PhysicalFileSystem(directory), this);
        }

        private IEnumerable<IPackageRepository> GetRepositories() {
            return GetRepositoryPaths().Select(CreateRepository);
        }

        internal IEnumerable<string> GetRepositoryPaths() {
            // The store file is in this format
            // <repositories>
            //     <repository path="..\packages.config" />
            // </repositories>
            XDocument document = GetStoreDocument();

            // The document doesn't exist so do nothing
            if (document == null) {
                return Enumerable.Empty<string>();
            }

            // Only save if we changed the document
            bool requiresSave = false;

            // Paths have to be relative to the this repository           
            var paths = new HashSet<string>();
            foreach (var e in GetRepositoryElements(document).ToList()) {
                string path = NormalizePath(e.GetOptionalAttributeValue("path"));

                if (String.IsNullOrEmpty(path) ||
                    !FileSystem.FileExists(path) ||
                    !paths.Add(path)) {

                    // Skip bad entries
                    e.Remove();
                    requiresSave = true;
                }
            }

            if (requiresSave) {
                SaveDocument(document);
            }

            return paths;
        }

        private void AddEntry(string path) {
            path = NormalizePath(path);

            // Create the document if it doesn't exist
            XDocument document = GetStoreDocument(createIfNotExists: true);

            XElement element = FindEntry(document, path);

            if (element != null) {
                // The path exists already so do nothing
                return;
            }

            document.Root.Add(new XElement("repository",
                                  new XAttribute("path", path)));

            SaveDocument(document);
        }

        private void DeleteEntry(string path) {
            // Get the relative path
            path = NormalizePath(path);

            // Remove the repository from the document
            XDocument document = GetStoreDocument();

            if (document == null) {
                return;
            }

            XElement element = FindEntry(document, path);

            if (element != null) {
                element.Remove();

                // No more entries so remove the file
                if (!document.Root.HasElements) {
                    FileSystem.DeleteFile(StoreFilePath);
                }
                else {
                    SaveDocument(document);
                }
            }
        }

        private static IEnumerable<XElement> GetRepositoryElements(XDocument document) {
            return from e in document.Root.Elements("repository")
                   select e;
        }

        private XElement FindEntry(XDocument document, string path) {
            path = NormalizePath(path);

            return (from e in GetRepositoryElements(document)
                    let entryPath = NormalizePath(e.GetOptionalAttributeValue("path"))
                    where path.Equals(entryPath, StringComparison.OrdinalIgnoreCase)
                    select e).FirstOrDefault();
        }

        private void SaveDocument(XDocument document) {
            FileSystem.AddFile(StoreFilePath, document.Save);
        }

        private XDocument GetStoreDocument(bool createIfNotExists = false) {
            try {
                // If the file exists then open and return it
                if (FileSystem.FileExists(StoreFilePath)) {
                    using (Stream stream = FileSystem.OpenFile(StoreFilePath)) {
                        try {
                            return XDocument.Load(stream);
                        }
                        catch (XmlException) {
                            // There was an error reading the file, but don't throw as a result
                        }
                    }
                }

                // If it doesn't exist and we're creating a new file then return a
                // document with an empty packages node
                if (createIfNotExists) {
                    return new XDocument(new XElement("repositories"));
                }

                return null;
            }
            catch (Exception e) {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                                  NuGetResources.ErrorReadingFile,
                                  FileSystem.GetFullPath(StoreFilePath)), e);
            }
        }

        private string NormalizePath(string path) {
            if (String.IsNullOrEmpty(path)) {
                return path;
            }

            if (Path.IsPathRooted(path)) {
                string root = PathUtility.EnsureTrailingSlash(FileSystem.Root);
                return PathUtility.GetRelativePath(root, path);
            }
            return path;
        }
    }
}

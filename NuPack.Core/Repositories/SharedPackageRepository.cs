using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuGet {
    public class SharedPackageRepository : LocalPackageRepository, ISharedPackageRepository {
        private const string StoreFilePath = "repositories.config";

        private readonly Dictionary<string, RepositoryCacheEntry> _repositoryCache = new Dictionary<string, RepositoryCacheEntry>(StringComparer.OrdinalIgnoreCase);

        public SharedPackageRepository(string path)
            : base(path) {
        }

        public SharedPackageRepository(IPackagePathResolver resolver, IFileSystem fileSystem)
            : base(resolver, fileSystem) {
        }

        public void RegisterRepository(string path) {
            // Add the entry to the document
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
            string directory = FileSystem.GetFullPath(Path.GetDirectoryName(path));
            return new PackageReferenceRepository(new PhysicalFileSystem(directory), this);
        }

        private IEnumerable<IPackageRepository> GetRepositories() {
            foreach (var path in GetRepositoryPaths()) {
                RepositoryCacheEntry cacheEntry;
                DateTimeOffset lastModified = FileSystem.GetLastModified(path);
                // If we never cached this file or we did and it's current last modified time is newer
                // create a new entry
                if (!_repositoryCache.TryGetValue(path, out cacheEntry) ||
                    (cacheEntry != null && lastModified > cacheEntry.LastModifiedTime)) {
                    // Create the repository
                    IPackageRepository repository = CreateRepository(path);

                    // create a cache entry with the last modified time
                    cacheEntry = new RepositoryCacheEntry(repository, lastModified);

                    // Store the entry
                    _repositoryCache[path] = cacheEntry;
                }

                yield return cacheEntry.Repository;
            }
        }

        private IEnumerable<string> GetRepositoryPaths() {
            // The store file is in this format
            // <repositories>
            //     <repository path="..\packages.config" />
            // </repositories>
            XDocument document = GetStoreDocument();

            // The document doesn't exist so do nothing
            if (document == null) {
                yield break;
            }

            // Paths have to be relative to the this repository
            var entries = from e in GetRepositoryElements(document)
                          select new {
                              Element = e,
                              Path = e.GetOptionalAttributeValue("path")
                          };

            foreach (var entry in entries.ToList()) {
                string path = NormalizePath(entry.Path);

                if (String.IsNullOrEmpty(path) || !FileSystem.FileExists(path)) {
                    // Remove invalid entries from the document
                    entry.Element.Remove();
                }
                else {
                    yield return path;
                }
            }

            SaveDocument(document);
        }

        private void AddEntry(string path) {
            path = NormalizePath(path);

            // If we already have this path then do nothing
            if (_repositoryCache.ContainsKey(path)) {
                return;
            }

            // Create the document if it doesn't exist
            XDocument document = GetStoreDocument(createIfNotExists: true);

            XElement element = FindEntry(document, path);

            if (element != null) {
                element.Remove();
            }

            document.Root.Add(new XElement("repository",
                                  new XAttribute("path", path)));

            SaveDocument(document);
        }

        private void DeleteEntry(string path) {
            // Get the relative path
            path = NormalizePath(path);

            // Remove the entry from the cache
            _repositoryCache.Remove(path);

            // Remove the repository from the document
            XDocument document = GetStoreDocument();

            if (document == null) {
                return;
            }

            XElement element = FindEntry(document, path);

            if (element != null) {
                element.Remove();
            }

            // REVIEW: Should we remove the file if no projects reference this repository?
            SaveDocument(document);
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
            // If the file exists then open and return it
            if (FileSystem.FileExists(StoreFilePath)) {
                using (Stream stream = FileSystem.OpenFile(StoreFilePath)) {
                    return XDocument.Load(stream);
                }
            }

            // If it doesn't exist and we're creating a new file then return a
            // document with an empty packages node
            if (createIfNotExists) {
                return new XDocument(new XElement("repositories"));
            }

            return null;
        }

        private string NormalizePath(string path) {
            if (String.IsNullOrEmpty(path)) {
                return path;
            }

            if (Path.IsPathRooted(path)) {
                return GetRelativePath(path);
            }
            return path;
        }

        private string GetRelativePath(string path) {
            return PathUtility.GetRelativePath(FileSystem.Root, path);
        }

        private class RepositoryCacheEntry {
            public RepositoryCacheEntry(IPackageRepository repository, DateTimeOffset lastModifiedTime) {
                Repository = repository;
                LastModifiedTime = lastModifiedTime;
            }

            public IPackageRepository Repository {
                get;
                private set;
            }

            public DateTimeOffset LastModifiedTime {
                get;
                private set;
            }
        }
    }
}

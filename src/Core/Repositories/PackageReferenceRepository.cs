using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NuGet.Resources;

namespace NuGet {
    /// <summary>
    /// This repository implementation keeps track of packages that are referenced in a project but
    /// it also has a reference to the repository that actually contains the packages. It keeps track
    /// of packages in an xml file at the project root (packages.xml).
    /// </summary>
    public class PackageReferenceRepository : PackageRepositoryBase {
        private const string PackageReferenceFile = "packages.config";

        public PackageReferenceRepository(IFileSystem fileSystem, ISharedPackageRepository sourceRepository) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            FileSystem = fileSystem;
            SourceRepository = sourceRepository;
        }

        public override string Source {
            get {
                return SourceRepository.Source;
            }
        }

        private IFileSystem FileSystem {
            get;
            set;
        }

        private ISharedPackageRepository SourceRepository {
            get;
            set;
        }

        private string PackageReferenceFileFullPath {
            get {
                return FileSystem.GetFullPath(PackageReferenceFile);
            }
        }

        private XDocument GetDocument(bool createIfNotExists = false) {
            try {
                // If the file exists then open and return it
                if (FileSystem.FileExists(PackageReferenceFile)) {
                    using (Stream stream = FileSystem.OpenFile(PackageReferenceFile)) {
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
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.ErrorReadingFile, PackageReferenceFileFullPath), e);
            }
        }

        public override IQueryable<IPackage> GetPackages() {
            return GetPackagesCore().AsSafeQueryable();
        }

        private IEnumerable<IPackage> GetPackagesCore() {
            XDocument document = GetDocument();

            if (document == null) {
                yield break;
            }
            else {
                foreach (var e in document.Root.Elements("package")) {
                    string id = e.GetOptionalAttributeValue("id");
                    string versionString = e.GetOptionalAttributeValue("version");
                    Version version = VersionUtility.ParseOptionalVersion(versionString);
                    IPackage package = null;

                    if (String.IsNullOrEmpty(id) || 
                        version == null || 
                        !SourceRepository.TryFindPackage(id, version, out package)) {

                        // Skip bad entries
                        continue;
                    }
                    else {
                        yield return package;
                    }
                }
            }
        }

        public override void AddPackage(IPackage package) {
            XDocument document = GetDocument(createIfNotExists: true);

            AddEntry(document, package.Id, package.Version);
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

            // Notify the source repository every time we add a new package to the repository.
            // This doesn't really need to happen on every package add, but this is over agressive
            // to combat scenarios where the 2 repositories get out of sync. If this repository is already 
            // registered in the source then this will be ignored
            SourceRepository.RegisterRepository(PackageReferenceFileFullPath);
        }

        public override void RemovePackage(IPackage package) {
            XDocument document = GetDocument();

            // If there is no document then do nothing
            if (document == null) {
                return;
            }

            DeleteEntry(document, package.Id, package.Version);
        }

        public void RegisterIfNecessary() {
            if (GetPackages().Any()) {
                SourceRepository.RegisterRepository(PackageReferenceFileFullPath);
            }
        }

        private void DeleteEntry(XDocument document, string id, Version version) {
            XElement element = FindEntry(document, id, version);

            if (element != null) {
                // Remove the element from the xml dom
                element.Remove();

                // Remove the file if there are no more elements
                if (!document.Root.HasElements) {
                    FileSystem.DeleteFile(PackageReferenceFile);

                    // Remove the repository from the source
                    SourceRepository.UnregisterRepository(PackageReferenceFileFullPath);
                }
                else {
                    // Otherwise save the updated document
                    SaveDocument(document);
                }
            }
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
            FileSystem.AddFile(PackageReferenceFile, document.Save);
        }
    }
}
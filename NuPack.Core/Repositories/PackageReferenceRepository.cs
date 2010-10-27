namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// This repository implementation keeps track of packages that are referenced in a project but
    /// it also has a reference to the repository that actually contains the packages. It keeps track
    /// of packages in an xml file at the project root (packages.xml).
    /// </summary>
    public class PackageReferenceRepository : PackageRepositoryBase {
        public PackageReferenceRepository(ProjectSystem project, IPackageRepository sourceRepository) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            Project = project;
            SourceRepository = sourceRepository;
        }

        private ProjectSystem Project {
            get;
            set;
        }

        private IPackageRepository SourceRepository {
            get;
            set;
        }

        private XDocument GetDocument(bool createIfNotExists = false) {
            // If the file exists then open and return it
            if (Project.FileExists(Constants.PackageReferenceFile)) {
                using (Stream stream = Project.OpenFile(Constants.PackageReferenceFile)) {
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

        public override IQueryable<IPackage> GetPackages() {
            IEnumerable<IPackage> packages;

            XDocument document = GetDocument();
            if (document == null) {
                packages = Enumerable.Empty<IPackage>();
            }
            else {
                packages = from packageElement in document.Root.Elements("package")
                           let id = packageElement.Attribute("id").Value
                           let version = Version.Parse(packageElement.Attribute("version").Value)
                           let package = SourceRepository.FindPackage(id, version)
                           where package != null
                           select package;
            }
            return packages.AsQueryable();
        }

        public override void AddPackage(IPackage package) {
            XDocument document = GetDocument(createIfNotExists: true);

            document.Root.Add(new XElement("package",
                                            new XAttribute("id", package.Id),
                                            new XAttribute("version", package.Version)));

            SaveDocument(document);
        }

        public override void RemovePackage(IPackage package) {
            XDocument document = GetDocument();

            // If there is no document then do nothing
            if (document == null) {
                return;
            }

            XElement packageElement = (from e in document.Root.Elements("package")
                                       let id = e.Attribute("id").Value
                                       let version = Version.Parse(e.Attribute("version").Value)
                                       where package.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                                             package.Version.Equals(version)
                                       select e).FirstOrDefault();

            Debug.Assert(packageElement != null, "Unable to find package in package file");

            // Remove the element from the xml dom
            packageElement.Remove();

            // Remove the file if there are no more elements
            if (!document.Root.HasElements) {
                Project.DeleteFile(Constants.PackageReferenceFile);
            }
            else {
                // Otherwise save the updated document
                SaveDocument(document);
            }
        }

        private void SaveDocument(XDocument document) {
            Project.AddFile(Constants.PackageReferenceFile, document.Save);
        }
    }
}

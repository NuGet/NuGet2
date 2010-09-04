namespace NuPack.Test {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Xml.Linq;
    using Moq;
    using Opc = System.IO.Packaging;

    internal class PackageUtility {
        private const string DefaultContentType = "application/octet";
        // REVIEW: Should we append a unique prefix?
        private const string ApplicationPackagePrefix = "http://schemas.microsoft.com/net/package/2010/";
        private const string ConfigurationRelationshipType = ApplicationPackagePrefix + "configuration";
        private const string DependenciesRelationshipType = ApplicationPackagePrefix + "dependencies";
        private const string ReferencesRelationshipType = ApplicationPackagePrefix + "reference";
        private const string ContentRelationshipType = ApplicationPackagePrefix + "content";
        private const string ToolRelationshipType = ApplicationPackagePrefix + "tool";

        internal static Stream CreateOpcPackageStream(string id, string version, string[] files = null, string[] assemblyReferences = null, string[] tools = null, string configPath = null, IEnumerable<PackageDependency> dependencies = null) {
            files = files ?? new string[0];
            assemblyReferences = assemblyReferences ?? new string[0];
            tools = tools ?? new string[0];
            dependencies = dependencies ?? Enumerable.Empty<PackageDependency>();

            MemoryStream memoryStream = new MemoryStream();
            using (var package = Opc.Package.Open(memoryStream, FileMode.Create)) {
                package.PackageProperties.Identifier = id;
                package.PackageProperties.Version = version;

                if (!String.IsNullOrEmpty(configPath)) {
                    Uri uri = UriHelper.CreatePartUri(configPath);
                    package.CreatePart(uri, DefaultContentType);
                    package.CreateRelationship(uri, Opc.TargetMode.Internal, ConfigurationRelationshipType);
                }

                foreach (var file in files) {
                    Uri uri = UriHelper.CreatePartUri(file);
                    package.CreatePart(uri, DefaultContentType);
                    package.CreateRelationship(uri, Opc.TargetMode.Internal, ContentRelationshipType);
                }

                foreach (var reference in assemblyReferences) {
                    Uri uri = UriHelper.CreatePartUri(reference);
                    package.CreatePart(uri, DefaultContentType);
                    package.CreateRelationship(uri, Opc.TargetMode.Internal, ReferencesRelationshipType);
                }

                foreach (var toolPath in tools) {
                    Uri uri = UriHelper.CreatePartUri(toolPath);
                    package.CreatePart(uri, DefaultContentType);
                    package.CreateRelationship(uri, Opc.TargetMode.Internal, ToolRelationshipType);
                }

                XDocument dependencyDocument = new XDocument(new XElement("Dependencies", from d in dependencies
                                                                                          select CreateDependencyElement(d)));
                Uri dependencyUri = UriHelper.CreatePartUri("dependencies.xml");
                Opc.PackagePart dependencyPart = package.CreatePart(dependencyUri, DefaultContentType);
                package.CreateRelationship(dependencyUri, Opc.TargetMode.Internal, DependenciesRelationshipType);
                using (Stream dependencyStream = dependencyPart.GetStream()) {
                    dependencyDocument.Save(dependencyStream);
                }
            }

            return memoryStream;
        }

        private static XElement CreateDependencyElement(PackageDependency dependency) {
            var dependencyElement = new XElement("Dependency",
                                            new XAttribute("Id", dependency.Id));
            if (dependency.Version != null) {
                dependencyElement.Add(new XAttribute("Version", dependency.Version));
            }
            else {
                if (dependency.MinVersion != null) {
                    dependencyElement.Add(new XAttribute("MinVersion", dependency.MinVersion));
                }
                if (dependency.MaxVersion != null) {
                    dependencyElement.Add(new XAttribute("MaxVersion", dependency.MaxVersion));
                }
            }

            return dependencyElement;
        }

        internal static Package CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content = null,
                                              IEnumerable<string> assemblyReferences = null,
                                              IEnumerable<string> resources = null,
                                              IEnumerable<PackageDependency> dependencies = null) {
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<string>();
            return CreatePackage(id,
                                 version,
                                 content,
                                 CreateAssemblyReferences(assemblyReferences),
                                 resources,
                                 dependencies);
        }

        internal static Package CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content,
                                              IEnumerable<IPackageAssemblyReference> assemblyReferences,
                                              IEnumerable<string> resources,
                                              IEnumerable<PackageDependency> dependencies) {
            content = content ?? Enumerable.Empty<string>();
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>();
            dependencies = dependencies ?? Enumerable.Empty<PackageDependency>();
            resources = resources ?? Enumerable.Empty<string>();

            var allFiles = new List<IPackageFile>();
            allFiles.AddRange(CreateFiles(content, "content"));
            allFiles.AddRange(CreateFiles(resources, "resources"));
            allFiles.AddRange(assemblyReferences);

            var mockPackage = new Mock<Package>() { CallBase = true };
            mockPackage.Setup(m => m.Id).Returns(id);
            mockPackage.Setup(m => m.Version).Returns(new Version(version));
            mockPackage.Setup(m => m.GetFiles()).Returns(allFiles);
            mockPackage.Setup(m => m.AssemblyReferences).Returns(assemblyReferences);
            mockPackage.Setup(m => m.Dependencies).Returns(dependencies);
            return mockPackage.Object;
        }

        private static List<IPackageAssemblyReference> CreateAssemblyReferences(IEnumerable<string> fileNames) {
            var assemblyReferences = new List<IPackageAssemblyReference>();
            foreach (var fileName in fileNames) {
                var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
                mockAssemblyReference.Setup(m => m.Open()).Returns(new MemoryStream());
                mockAssemblyReference.Setup(m => m.Path).Returns(fileName);
                mockAssemblyReference.Setup(m => m.Name).Returns(Path.GetFileName(fileName));
                assemblyReferences.Add(mockAssemblyReference.Object);
            }
            return assemblyReferences;
        }

        internal static IPackageAssemblyReference CreateAssemblyReference(string path, FrameworkName targetFramework) {
            var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
            mockAssemblyReference.Setup(m => m.Open()).Returns(new MemoryStream());
            mockAssemblyReference.Setup(m => m.Path).Returns(path);
            mockAssemblyReference.Setup(m => m.Name).Returns(path);
            mockAssemblyReference.Setup(m => m.TargetFramework).Returns(targetFramework);
            return mockAssemblyReference.Object;
        }

        internal static List<IPackageFile> CreateFiles(IEnumerable<string> fileNames, string directory = "") {
            var files = new List<IPackageFile>();
            foreach (var fileName in fileNames) {
                string path = Path.Combine(directory, fileName);
                var mockFile = new Mock<IPackageFile>();
                mockFile.Setup(m => m.Path).Returns(path);
                mockFile.Setup(m => m.Open()).Returns(() => new MemoryStream(Encoding.Default.GetBytes(path)));
                files.Add(mockFile.Object);
            }
            return files;
        }

        internal static IPackageAssemblyPathResolver CreateAssemblyResolver() {
            var mockAssemblyResolver = new Mock<IPackageAssemblyPathResolver>();
            mockAssemblyResolver.Setup(m => m.GetAssemblyPath(It.IsAny<Package>(),
                                                              It.IsAny<IPackageAssemblyReference>()))
                                .Returns<Package, IPackageAssemblyReference>((_, reference) => Path.Combine("FullPath", reference.Path));
            return mockAssemblyResolver.Object;
        }
    }
}

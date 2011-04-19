namespace NuGet.Test {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Text;
    using Moq;

    public class PackageUtility {
        public static IPackage CreateProjectLevelPackage(string id, string version = "1.0", IEnumerable<PackageDependency> dependencies = null) {
            return CreatePackage(id, version, assemblyReferences: new[] { id + ".dll" }, dependencies: dependencies);
        }

        public static IPackage CreatePackage(string id,
                                              string version = "1.0",
                                              IEnumerable<string> content = null,
                                              IEnumerable<string> assemblyReferences = null,
                                              IEnumerable<string> tools = null,
                                              IEnumerable<PackageDependency> dependencies = null,
                                              double? rating = null,
                                              string description = null) {
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<string>();
            return CreatePackage(id,
                                 version,
                                 content,
                                 CreateAssemblyReferences(assemblyReferences),
                                 tools,
                                 dependencies, 
                                 rating, 
                                 description);
        }

        public static IPackage CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content,
                                              IEnumerable<IPackageAssemblyReference> assemblyReferences,
                                              IEnumerable<string> tools,
                                              IEnumerable<PackageDependency> dependencies,
                                              double? rating,
                                              string description) {
            content = content ?? Enumerable.Empty<string>();
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>();
            dependencies = dependencies ?? Enumerable.Empty<PackageDependency>();
            tools = tools ?? Enumerable.Empty<string>();
            description = description ?? "Mock package " + id;

            var allFiles = new List<IPackageFile>();
            allFiles.AddRange(CreateFiles(content, "content"));
            allFiles.AddRange(CreateFiles(tools, "tools"));
            allFiles.AddRange(assemblyReferences);

            var mockPackage = new Mock<IPackage>() { CallBase = true };
            mockPackage.Setup(m => m.Id).Returns(id);
            mockPackage.Setup(m => m.Version).Returns(new Version(version));
            mockPackage.Setup(m => m.GetFiles()).Returns(allFiles);
            mockPackage.Setup(m => m.AssemblyReferences).Returns(assemblyReferences);
            mockPackage.Setup(m => m.Dependencies).Returns(dependencies);
            mockPackage.Setup(m => m.Description).Returns(description);
            mockPackage.Setup(m => m.Language).Returns("en-US");
            mockPackage.Setup(m => m.Authors).Returns(new[] { "Tester" });
            mockPackage.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
            mockPackage.Setup(m => m.LicenseUrl).Returns(new Uri("ftp://test/somelicense.txts"));
            mockPackage.Setup(m => m.Rating).Returns(rating ?? -1);
            return mockPackage.Object;
        }

        private static List<IPackageAssemblyReference> CreateAssemblyReferences(IEnumerable<string> fileNames) {
            var assemblyReferences = new List<IPackageAssemblyReference>();
            foreach (var fileName in fileNames) {
                var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
                mockAssemblyReference.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
                mockAssemblyReference.Setup(m => m.Path).Returns(fileName);
                mockAssemblyReference.Setup(m => m.Name).Returns(Path.GetFileName(fileName));
                assemblyReferences.Add(mockAssemblyReference.Object);
            }
            return assemblyReferences;
        }

        public static IPackageAssemblyReference CreateAssemblyReference(string path, FrameworkName targetFramework) {
            var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
            mockAssemblyReference.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
            mockAssemblyReference.Setup(m => m.Path).Returns(path);
            mockAssemblyReference.Setup(m => m.Name).Returns(path);
            mockAssemblyReference.Setup(m => m.TargetFramework).Returns(targetFramework);
            mockAssemblyReference.Setup(m => m.SupportedFrameworks).Returns(new[] { targetFramework });
            return mockAssemblyReference.Object;
        }

        public static List<IPackageFile> CreateFiles(IEnumerable<string> fileNames, string directory = "") {
            var files = new List<IPackageFile>();
            foreach (var fileName in fileNames) {
                string path = Path.Combine(directory, fileName);
                var mockFile = new Mock<IPackageFile>();
                mockFile.Setup(m => m.Path).Returns(path);
                mockFile.Setup(m => m.GetStream()).Returns(() => new MemoryStream(Encoding.Default.GetBytes(path)));
                files.Add(mockFile.Object);
            }
            return files;
        }
    }
}

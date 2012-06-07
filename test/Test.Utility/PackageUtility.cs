namespace NuGet.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Text;
    using Moq;

    public class PackageUtility
    {
        public static IPackage CreateProjectLevelPackage(string id, string version = "1.0", IEnumerable<PackageDependency> dependencies = null)
        {
            return CreatePackage(id, version, assemblyReferences: new[] { id + ".dll" }, dependencies: dependencies);
        }

        public static IPackage CreatePackage(string id,
                                              string version = "1.0",
                                              IEnumerable<string> content = null,
                                              IEnumerable<string> assemblyReferences = null,
                                              IEnumerable<string> tools = null,
                                              IEnumerable<PackageDependency> dependencies = null,
                                              int downloadCount = 0,
                                              string description = null,
                                              string summary = null,
                                              bool listed = true,
                                              string tags = "",
                                              string language = null,
                                              IEnumerable<string> satelliteAssemblies = null)
        {
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<string>();
            satelliteAssemblies = satelliteAssemblies ?? Enumerable.Empty<string>();

            return CreatePackage(id,
                                 version,
                                 content,
                                 CreateAssemblyReferences(assemblyReferences),
                                 tools,
                                 dependencies,
                                 downloadCount,
                                 description,
                                 summary,
                                 listed,
                                 tags,
                                 language,
                                 CreateAssemblyReferences(satelliteAssemblies));
        }

        public static IPackage CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content,
                                              IEnumerable<IPackageAssemblyReference> assemblyReferences,
                                              IEnumerable<string> tools,
                                              IEnumerable<PackageDependency> dependencies,
                                              int downloadCount,
                                              string description,
                                              string summary,
                                              bool listed,
                                              string tags)
        {
            return CreatePackage(id,
                                 version,
                                 content,
                                 assemblyReferences,
                                 tools,
                                 dependencies,
                                 downloadCount,
                                 description,
                                 summary,
                                 listed,
                                 tags,
                                 language: null,
                                 satelliteAssemblies: null);
        }

        public static IPackage CreatePackageWithDependencySets(string id,
                                              string version = "1.0",
                                              IEnumerable<string> content = null,
                                              IEnumerable<string> assemblyReferences = null,
                                              IEnumerable<string> tools = null,
                                              IEnumerable<PackageDependencySet> dependencySets = null,
                                              int downloadCount = 0,
                                              string description = null,
                                              string summary = null,
                                              bool listed = true,
                                              string tags = "",
                                              string language = null,
                                              IEnumerable<string> satelliteAssemblies = null)
        {
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<string>();
            satelliteAssemblies = satelliteAssemblies ?? Enumerable.Empty<string>();

            return CreatePackage(id,
                                 version,
                                 content,
                                 CreateAssemblyReferences(assemblyReferences),
                                 tools,
                                 dependencySets,
                                 downloadCount,
                                 description,
                                 summary,
                                 listed,
                                 tags,
                                 language,
                                 CreateAssemblyReferences(satelliteAssemblies));
        }

        public static IPackage CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content,
                                              IEnumerable<IPackageAssemblyReference> assemblyReferences,
                                              IEnumerable<string> tools,
                                              IEnumerable<PackageDependency> dependencies,
                                              int downloadCount,
                                              string description,
                                              string summary,
                                              bool listed,
                                              string tags,
                                              string language,
                                              IEnumerable<IPackageAssemblyReference> satelliteAssemblies)
        {
            var dependencySets = new List<PackageDependencySet>
            {
                new PackageDependencySet(null, dependencies ?? Enumerable.Empty<PackageDependency>())
            };

            return CreatePackage(id,
                                 version,
                                 content,
                                 assemblyReferences,
                                 tools,
                                 dependencySets,
                                 downloadCount,
                                 description,
                                 summary,
                                 listed,
                                 tags,
                                 language,
                                 satelliteAssemblies);
        }

        public static IPackage CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content,
                                              IEnumerable<IPackageAssemblyReference> assemblyReferences,
                                              IEnumerable<string> tools,
                                              IEnumerable<PackageDependencySet> dependencySets,
                                              int downloadCount,
                                              string description,
                                              string summary,
                                              bool listed,
                                              string tags,
                                              string language,
                                              IEnumerable<IPackageAssemblyReference> satelliteAssemblies)
        {
            content = content ?? Enumerable.Empty<string>();
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>();
            satelliteAssemblies = satelliteAssemblies ?? Enumerable.Empty<IPackageAssemblyReference>();
            dependencySets = dependencySets ?? Enumerable.Empty<PackageDependencySet>();
            tools = tools ?? Enumerable.Empty<string>();
            description = description ?? "Mock package " + id;

            var allFiles = new List<IPackageFile>();
            allFiles.AddRange(CreateFiles(content, "content"));
            allFiles.AddRange(CreateFiles(tools, "tools"));
            allFiles.AddRange(assemblyReferences);
            allFiles.AddRange(satelliteAssemblies);

            var mockPackage = new Mock<IPackage>(MockBehavior.Strict) { CallBase = true };
            mockPackage.Setup(m => m.IsAbsoluteLatestVersion).Returns(true);
            mockPackage.Setup(m => m.IsLatestVersion).Returns(String.IsNullOrEmpty(SemanticVersion.Parse(version).SpecialVersion));
            mockPackage.Setup(m => m.Id).Returns(id);
            mockPackage.Setup(m => m.Listed).Returns(true);
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion(version));
            mockPackage.Setup(m => m.GetFiles()).Returns(allFiles);
            mockPackage.Setup(m => m.AssemblyReferences).Returns(assemblyReferences);
            mockPackage.Setup(m => m.DependencySets).Returns(dependencySets);
            mockPackage.Setup(m => m.Description).Returns(description);
            mockPackage.Setup(m => m.Language).Returns("en-US");
            mockPackage.Setup(m => m.Authors).Returns(new[] { "Tester" });
            mockPackage.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
            mockPackage.Setup(m => m.LicenseUrl).Returns(new Uri("ftp://test/somelicense.txts"));
            mockPackage.Setup(m => m.Summary).Returns(summary);
            mockPackage.Setup(m => m.FrameworkAssemblies).Returns(Enumerable.Empty<FrameworkAssemblyReference>());
            mockPackage.Setup(m => m.Tags).Returns(tags);
            mockPackage.Setup(m => m.Title).Returns(String.Empty);
            mockPackage.Setup(m => m.DownloadCount).Returns(downloadCount);
            mockPackage.Setup(m => m.RequireLicenseAcceptance).Returns(false);
            mockPackage.Setup(m => m.Listed).Returns(listed);
            mockPackage.Setup(m => m.Language).Returns(language);
            mockPackage.Setup(m => m.IconUrl).Returns((Uri)null);
            mockPackage.Setup(m => m.ProjectUrl).Returns((Uri)null);
            mockPackage.Setup(m => m.ReleaseNotes).Returns("");
            if (!listed)
            {
                mockPackage.Setup(m => m.Published).Returns(Constants.Unpublished);
            }
            
            return mockPackage.Object;
        }

        private static List<IPackageAssemblyReference> CreateAssemblyReferences(IEnumerable<string> fileNames)
        {
            var assemblyReferences = new List<IPackageAssemblyReference>();
            foreach (var fileName in fileNames)
            {
                var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
                mockAssemblyReference.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
                mockAssemblyReference.Setup(m => m.Path).Returns(fileName);
                mockAssemblyReference.Setup(m => m.Name).Returns(Path.GetFileName(fileName));


                string effectivePath;
                FrameworkName fn = ParseFrameworkName(fileName, out effectivePath);
                if (fn != null)
                {
                    mockAssemblyReference.Setup(m => m.EffectivePath).Returns(effectivePath);
                    mockAssemblyReference.Setup(m => m.TargetFramework).Returns(fn);
                    mockAssemblyReference.Setup(m => m.SupportedFrameworks).Returns(new[] { fn });
                }
                else
                {
                    mockAssemblyReference.Setup(m => m.EffectivePath).Returns(fileName);
                }

                assemblyReferences.Add(mockAssemblyReference.Object);
            }
            return assemblyReferences;
        }

        private static FrameworkName ParseFrameworkName(string fileName, out string effectivePath)
        {
            if (fileName.StartsWith("lib\\"))
            {
                fileName = fileName.Substring(4);
                return VersionUtility.ParseFrameworkFolderName(fileName, strictParsing: false, effectivePath: out effectivePath);
            }

            effectivePath = fileName;
            return null;
        }

        public static IPackageAssemblyReference CreateAssemblyReference(string path, FrameworkName targetFramework)
        {
            var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
            mockAssemblyReference.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
            mockAssemblyReference.Setup(m => m.Path).Returns(path);
            mockAssemblyReference.Setup(m => m.Name).Returns(path);
            mockAssemblyReference.Setup(m => m.TargetFramework).Returns(targetFramework);
            mockAssemblyReference.Setup(m => m.SupportedFrameworks).Returns(new[] { targetFramework });
            return mockAssemblyReference.Object;
        }

        public static List<IPackageFile> CreateFiles(IEnumerable<string> fileNames, string directory = "")
        {
            var files = new List<IPackageFile>();
            foreach (var fileName in fileNames)
            {
                string path = Path.Combine(directory, fileName);
                var mockFile = new Mock<IPackageFile>();
                mockFile.Setup(m => m.Path).Returns(path);
                mockFile.Setup(m => m.GetStream()).Returns(() => new MemoryStream(Encoding.Default.GetBytes(path)));

                string effectivePath;
                FrameworkName fn = VersionUtility.ParseFrameworkNameFromFilePath(path, out effectivePath);
                mockFile.Setup(m => m.TargetFramework).Returns(fn);
                mockFile.Setup(m => m.EffectivePath).Returns(effectivePath);
                mockFile.Setup(m => m.SupportedFrameworks).Returns(
                    fn == null ? new FrameworkName[0] : new FrameworkName[] { fn });

                files.Add(mockFile.Object);
            }
            return files;
        }
    }
}
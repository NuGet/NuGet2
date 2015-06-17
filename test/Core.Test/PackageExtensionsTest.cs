using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageExtensionsTest
    {
        [Fact]
        public void FindPackagesOverloadLooksForSearchTermsInSpecificFields()
        {
            // Arrange
            var packages = new[] {
                PackageUtility.CreatePackage("Foo.Qux", description: "Some desc"),
                PackageUtility.CreatePackage("X-Package", tags: " lib qux "),
                PackageUtility.CreatePackage("Filtered"),
                PackageUtility.CreatePackage("B", description: "This is a package for qux and not one for baz"),
            };

            // Act
            var result1 = packages.AsQueryable().Find(new[] { "Description", "Tags" }, "Qux");
            var result2 = packages.AsQueryable().Find(new[] { "Id" }, "Filtered");

            // Assert
            Assert.Equal(new[] { packages[1], packages[3] }, result1.ToArray());
            Assert.Equal(new[] { packages[2], }, result2.ToArray());
        }

        [Fact]
        public void FindPackagesOverloadReturnsEmptySequenceIfTermIsNotFoundInProperties()
        {
            // Arrange
            var packages = new[] {
                PackageUtility.CreatePackage("Foo.Qux"),
                PackageUtility.CreatePackage("X-Package", tags: " lib qux "),
                PackageUtility.CreatePackage("Filtered"),
                PackageUtility.CreatePackage("B", description: "This is a package for qux and not one for baz"),
            };

            // Act
            var result1 = packages.AsQueryable().Find(new[] { "Summary" }, "Qux");

            // Assert
            Assert.Empty(result1);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsSatellitePackageReturnsFalseIfThePackageDoesNotHaveLanguageSet(string language)
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Foo", "1.0.0", language: language);

            // Act
            bool result = package.IsSatellitePackage();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("Foo", "ja-jp")]
        [InlineData("Foo.ja", "ja-jp")]
        [InlineData("Foo.ja.jp", "ja-jp")]
        [InlineData("Foo.ja-jp.test", "ja-jp")]
        public void IsSatellitePackageReturnsFalseIfThePackageIdDoesNotEndInLanguage(string id, string language)
        {
            // Arrange
            var package = PackageUtility.CreatePackage(id, "1.0.0", language: language);

            // Act
            bool result = package.IsSatellitePackage();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("Bar", "[1.0]")]
        [InlineData("ja-jp", "[1.0]")]
        [InlineData("Foo.ja-jp.test", "[1.0]")]
        [InlineData("Foo", "(1.0]")]
        public void IsSatellitePackageReturnsFalseIfThePackageCoreDependencyIsIncorrect(string dependencyId, string dependencyVersion)
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Foo.ja-jp", "1.0.0", language: "ja-jp",
                dependencies: new[] { new PackageDependency(dependencyId, VersionUtility.ParseVersionSpec(dependencyVersion)) });

            // Act
            bool result = package.IsSatellitePackage();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseIfThePackageHasNoDependencies()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Foo", "1.0.0", language: "ja-jp");

            // Act
            bool result = package.IsSatellitePackage();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSatellitePackageReturnsTrueIfThePackageHasStrongCoreDependency()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Foo.ja-jp", "1.0.0", language: "ja-jp",
                dependencies: new[] { new PackageDependency("Foo", VersionUtility.ParseVersionSpec("[1.0]")) });

            // Act
            bool result = package.IsSatellitePackage();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Create a package with teh specified language and file, and expect that the file is treated
        /// as a satellite file.
        /// </summary>
        /// <param name="language">The language for the package.</param>
        /// <param name="file">The file expected to be matched as a satellite file.</param>
        [Theory]
        [InlineData(new object[] { "ja-jp", @"lib\ja-jp\assembly.dll" })]
        [InlineData(new object[] { "ja-jp", @"lib\foo\ja-jp\assembly.dll" })]
        [InlineData(new object[] { "ja-jp", @"lib\foo\ja-jp\bar\assembly.dll" })]
        [InlineData(new object[] { "ja-jp", @"lib\ja-jp\bar\assembly.dll" })]
        [InlineData(new object[] { "ja-JP", @"lib\ja-jp\assembly.dll" })] // case mismatch still works though
        public void GetSatelliteFilesReturnsFilesWithAnyCultureSubFolder(string language, string file)
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Foo", "1.0.0", assemblyReferences: new[] { file }, language: language);

            // Act
            var satelliteFiles = package.GetSatelliteFiles();

            // Assert
            Assert.True(satelliteFiles.Select(f => f.Path).Contains(file));
        }

        /// <summary>
        /// Create a package with the specified language and file, and expect that the file is not treated
        /// as a satellite file.
        /// </summary>
        /// <param name="language">The language for the package.</param>
        /// <param name="file">The file expected not to be matched as a satellite file.</param>
        [Theory]
        [InlineData(new object[] { "ja-jp", @"lib\ja-jp" })] // a file with a name that matches the culture
        [InlineData(new object[] { "ja", @"lib\ja-jp\assembly.dll" })] // culture doesn't match
        [InlineData(new object[] { "fr-fr", @"lib\ja-jp\assembly.dll" })] // culture doesn't match
        [InlineData(new object[] { "ja-jp", @"ja-jp\assembly.dll" })] // not in the lib folder
        [InlineData(new object[] { "ja-jp", @"content\ja-jp\assembly.dll" })] // not in the lib folder
        public void GetSatelliteFilesDoesNotReturnFilesOutsideOfCultureSubfolder(string language, string file)
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Foo", "1.0.0", assemblyReferences: new[] { file }, language: language);

            // Act
            var satelliteFiles = package.GetSatelliteFiles();

            // Assert
            Assert.False(satelliteFiles.Select(f => f.Path).Contains(file));
        }

        [Theory]
        [InlineData("content\\_._")]
        [InlineData("lib\\_._")]
        [InlineData("content\\sub\\_._")]
        [InlineData("content\\sub\\child\\_._")]
        [InlineData("_._")]
        public void TestIsEmptyFolderReturnTrue(string path)
        {
            // Arrange
            var package = new Mock<IPackageFile>();
            package.Setup(p => p.Path).Returns(path);

            // Act
            bool isEmptyFolder = package.Object.IsEmptyFolder();

            // Assert
            Assert.True(isEmptyFolder);
        }

        [Theory]
        [InlineData("content\\_._1")]
        [InlineData("lib\\one.xml")]
        [InlineData("content")]
        [InlineData("content\\sub\\child\\_2._")]
        [InlineData("_.3_")]
        [InlineData("___")]
        public void TestIsEmptyFolderReturnFalse(string path)
        {
            // Arrange
            var package = new Mock<IPackageFile>();
            package.Setup(p => p.Path).Returns(path);

            // Act
            bool isEmptyFolder = package.Object.IsEmptyFolder();

            // Assert
            Assert.False(isEmptyFolder);
        }

        [Fact]
        public void GetContentFiles_WithoutManagedCodeConventions_ReturnsFilesUnderContentDirectory()
        {
            // Arrange
            var packageFiles = new List<IPackageFile>();
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "PackageRoot.txt", "PackageRoot-Hyphenated.config", "Content-Type.xml" }, useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "ContentRoot.txt", "ContentRoot2.txt", "net45\\Net45Content.txt" }, "content", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Contentnet45.txt" }, "content.net45", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Contentportable.txt" }, "content.portable-net45+wp8", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Content2Root.txt", "Content2Root2.txt" }, "content2", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Tools1.txt", "Tools2.txt" }, "tools", useManagedCodeConventions: false));
            var package = new Mock<IPackage>();
            package.Setup(p => p.PackageType).Returns(PackageType.Default);
            package.Setup(p => p.GetFiles()).Returns(packageFiles);

            // Act
            var files = package.Object.GetContentFiles().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal("content\\ContentRoot.txt", files[0].Path);
            Assert.Null(files[0].TargetFramework);
            Assert.Equal("content\\ContentRoot2.txt", files[1].Path);
            Assert.Null(files[1].TargetFramework);
            Assert.Equal("content\\net45\\Net45Content.txt", files[2].Path);
            Assert.Equal(".NETFramework,Version=v4.5", files[2].TargetFramework.ToString());
        }

        [Fact]
        public void GetContentFiles_WithManagedCodeConventions_ReturnsFilesUnderContentDirectory()
        {
            // Arrange
            var packageFiles = new List<IPackageFile>();
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "PackageRoot.txt", "PackageRoot-Hyphenated.config", "Content-Type.xml" }, useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "ContentRoot.txt", "ContentRoot2.txt", "net45\\Net45Content.txt" }, "content", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Contentnet45.txt" }, "content.net45", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "nested\\Contentportable.txt" }, "content.portable-net45+wp8", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Content2Root.txt", "Content2Root2.txt" }, "content2", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "BadFxContent.txt" }, "content.++++1133bad.fx", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Tools1.txt", "Tools2.txt" }, "tools", useManagedCodeConventions: true));
            var package = new Mock<IPackage>();
            package.Setup(p => p.PackageType).Returns(PackageType.Managed);
            package.Setup(p => p.GetFiles()).Returns(packageFiles);

            // Act
            var files = package.Object.GetContentFiles().ToList();

            // Assert
            Assert.Equal(6, files.Count);
            Assert.Equal("content\\ContentRoot.txt", files[0].Path);
            Assert.Equal(VersionUtility.EmptyFramework, files[0].TargetFramework);

            Assert.Equal("content\\ContentRoot2.txt", files[1].Path);
            Assert.Equal(VersionUtility.EmptyFramework, files[1].TargetFramework);

            Assert.Equal("content\\net45\\Net45Content.txt", files[2].Path);
            Assert.Equal("net45\\Net45Content.txt", files[2].EffectivePath);
            Assert.Equal(VersionUtility.EmptyFramework, files[2].TargetFramework);

            Assert.Equal("content.net45\\Contentnet45.txt", files[3].Path);
            Assert.Equal("Contentnet45.txt", files[3].EffectivePath);
            Assert.Equal(".NETFramework,Version=v4.5", files[3].TargetFramework.ToString());

            Assert.Equal("content.portable-net45+wp8\\nested\\Contentportable.txt", files[4].Path);
            Assert.Equal("nested\\Contentportable.txt", files[4].EffectivePath);
            Assert.Equal(".NETPortable,Version=v0.0,Profile=net45+wp8", files[4].TargetFramework.ToString());

            Assert.Equal("content.++++1133bad.fx\\BadFxContent.txt", files[5].Path);
            Assert.Equal("BadFxContent.txt", files[5].EffectivePath);
            Assert.Equal("++++1133bad.fx,Version=v0.0", files[5].TargetFramework.ToString());
        }

        [Fact]
        public void GetToolFiles_WithoutManagedCodeConventions_ReturnsFilesUnderContentDirectory()
        {
            // Arrange
            var packageFiles = new List<IPackageFile>();
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "PackageRoot.txt", "PackageRoot-Hyphenated.config", "Tools-Type.xml" }, useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "ToolsRoot.txt", "ToolsRoot2.txt", "net45\\Net45Tools.txt" }, "tools", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Toolsnet45.txt" }, "tools.net45", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Toolsportable.txt" }, "tools.portable-net45+wp8", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Tools2Root.txt", "Tools2Root2.txt" }, "tools2", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "content.txt", "content2.txt" }, "content", useManagedCodeConventions: false));
            var package = new Mock<IPackage>();
            package.Setup(p => p.PackageType).Returns(PackageType.Default);
            package.Setup(p => p.GetFiles()).Returns(packageFiles);

            // Act
            var files = package.Object.GetToolFiles().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal("tools\\ToolsRoot.txt", files[0].Path);
            Assert.Null(files[0].TargetFramework);
            Assert.Equal("tools\\ToolsRoot2.txt", files[1].Path);
            Assert.Null(files[1].TargetFramework);
            Assert.Equal("tools\\net45\\Net45Tools.txt", files[2].Path);
            Assert.Equal(".NETFramework,Version=v4.5", files[2].TargetFramework.ToString());
        }

        [Fact]
        public void GetToolFiles_WithManagedCodeConventions_ReturnsFilesUnderContentDirectory()
        {
            // Arrange
            var packageFiles = new List<IPackageFile>();
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "PackageRoot.txt", "PackageRoot-Hyphenated.config", "Tools-Type.xml" }, useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "ToolsRoot.txt", "ToolsRoot2.txt", "net45\\Net45Tools.txt" }, "tools", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Toolsnet45.txt" }, "tools.net45", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "nested\\Toolsportable.txt" }, "tools.portable-net45+wp8", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "Tools2Root.txt", "Tools2Root2.txt" }, "tools2", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "BadFxTools.txt" }, "tools.++++1133bad.fx", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "content.txt", "content2.txt" }, "content", useManagedCodeConventions: true));

            var package = new Mock<IPackage>();
            package.Setup(p => p.PackageType).Returns(PackageType.Managed);
            package.Setup(p => p.GetFiles()).Returns(packageFiles);

            // Act
            var files = package.Object.GetToolFiles().ToList();

            // Assert
            Assert.Equal(6, files.Count);
            Assert.Equal("tools\\ToolsRoot.txt", files[0].Path);
            Assert.Equal(VersionUtility.EmptyFramework, files[0].TargetFramework);

            Assert.Equal("tools\\ToolsRoot2.txt", files[1].Path);
            Assert.Equal(VersionUtility.EmptyFramework, files[1].TargetFramework);

            Assert.Equal("tools\\net45\\Net45Tools.txt", files[2].Path);
            Assert.Equal("net45\\Net45Tools.txt", files[2].EffectivePath);
            Assert.Equal(VersionUtility.EmptyFramework, files[2].TargetFramework);

            Assert.Equal("tools.net45\\Toolsnet45.txt", files[3].Path);
            Assert.Equal("Toolsnet45.txt", files[3].EffectivePath);
            Assert.Equal(".NETFramework,Version=v4.5", files[3].TargetFramework.ToString());

            Assert.Equal("tools.portable-net45+wp8\\nested\\Toolsportable.txt", files[4].Path);
            Assert.Equal("nested\\Toolsportable.txt", files[4].EffectivePath);
            Assert.Equal(".NETPortable,Version=v0.0,Profile=net45+wp8", files[4].TargetFramework.ToString());

            Assert.Equal("tools.++++1133bad.fx\\BadFxTools.txt", files[5].Path);
            Assert.Equal("BadFxTools.txt", files[5].EffectivePath);
            Assert.Equal("++++1133bad.fx,Version=v0.0", files[5].TargetFramework.ToString());
        }

        [Fact]
        public void GetBuildFiles_WithoutManagedCodeConventions_ReturnsTargetAndPropFilesUnderToolsDirectory_ThatMatchPackageId()
        {
            // Arrange
            var packageFiles = new List<IPackageFile>();
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "MyPackage.props", "MyPackage.targets", "Build-Type.xml" }, useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(
                new[] 
                { 
                    "MyPackage.props", 
                    "MyPackage.1.0.props", 
                    "MyPackage.1.0.targets",
                    "MyPackage.targets",
                    "net45\\MyPackage.props",
                    "net45\\MyPackage2.props",
                    "net45\\MyPackage.1.0.targets",
                }, "build", useManagedCodeConventions: false));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "MyPackage.props", "MyPackage40.targets" }, "build.net40", useManagedCodeConventions: false));
            var package = new Mock<IPackage>();
            package.SetupGet(p => p.Id).Returns("MyPackage");
            package.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            package.SetupGet(p => p.PackageType).Returns(PackageType.Default);
            package.Setup(p => p.GetFiles()).Returns(packageFiles);

            // Act
            var files = package.Object.GetBuildFiles().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal("build\\MyPackage.props", files[0].Path);
            Assert.Null(files[0].TargetFramework);
            Assert.Equal("build\\MyPackage.targets", files[1].Path);
            Assert.Null(files[1].TargetFramework);
            Assert.Equal("build\\net45\\MyPackage.props", files[2].Path);
            Assert.Equal("MyPackage.props", files[2].EffectivePath);
            Assert.Equal(".NETFramework,Version=v4.5", files[2].TargetFramework.ToString());
        }

        [Fact]
        public void GetBuildFiles_WithManagedCodeConventions_ReturnsTargetAndPropFilesUnderToolsDirectory_ThatMatchPackageId()
        {
            // Arrange
            var packageFiles = new List<IPackageFile>();
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "MyPackage.props", "MyPackage.targets", "Build-Type.xml" }, useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(
                new[] 
                { 
                    "MyPackage.props", 
                    "MyPackage.1.0.props", 
                    "MyPackage.1.0.targets",
                    "MyPackage.targets",
                    "net45\\MyPackage.props",
                    "net45\\MyPackage2.props",
                    "net45\\MyPackage.1.0.targets",
                }, "build", useManagedCodeConventions: true));
            packageFiles.AddRange(PackageUtility.CreateFiles(new[] { "MyPackage.props", "MyPackage40.targets" }, "build.net40", useManagedCodeConventions: true));
            var package = new Mock<IPackage>();
            package.SetupGet(p => p.Id).Returns("MyPackage");
            package.SetupGet(p => p.Version).Returns(new SemanticVersion("1.0.0"));
            package.SetupGet(p => p.PackageType).Returns(PackageType.Managed);
            package.Setup(p => p.GetFiles()).Returns(packageFiles);

            // Act
            var files = package.Object.GetBuildFiles().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal("build\\MyPackage.props", files[0].Path);
            Assert.Equal(VersionUtility.EmptyFramework, files[0].TargetFramework);
            Assert.Equal("build\\MyPackage.targets", files[1].Path);
            Assert.Equal(VersionUtility.EmptyFramework, files[1].TargetFramework);
            Assert.Equal("build.net40\\MyPackage.props", files[2].Path);
            Assert.Equal("MyPackage.props", files[2].EffectivePath);
            Assert.Equal(".NETFramework,Version=v4.0", files[2].TargetFramework.ToString());
        }
    }
}

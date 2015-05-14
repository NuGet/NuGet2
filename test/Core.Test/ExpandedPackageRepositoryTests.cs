using System.IO;
using System.Linq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class ExpandedPackageRepositoryTests
    {
        [Fact]
        public void GetPackages_ReturnsAllPackagesInsideDirectory()
        {
            // Arrange
            var fooPackage = new PackageBuilder
            {
                Id = "Foo",
                Version = new SemanticVersion("1.0.0"),
                Description = "Some description",
            };
            fooPackage.Authors.Add("test author");
            fooPackage.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"lib\net40", "Foo.dll", "lib contents").Object);

            var barPackage = new PackageBuilder
            {
                Id = "Bar",
                Version = new SemanticVersion("1.0.0-beta1"),
                Description = "Some description",
            };
            barPackage.Authors.Add("test author");
            barPackage.Files.Add(
                PackageUtility.CreateMockedPackageFile("", "README.md", "lib contents").Object);
            barPackage.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"content", "qwerty.js", "bar contents").Object);

            barPackage.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"lib\net451", "test.dll", "test-dll").Object);

            var fileSystem = new MockFileSystem();
            var fooRoot = Path.Combine("Foo", "1.0.0");
            fileSystem.AddFile(Path.Combine(fooRoot, "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.1.0.0.nupkg.sha512"), "Foo-sha");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.1.0.0.nupkg"), GetPackageStream(fooPackage));

            var barRoot = Path.Combine("Bar", "1.0.0-beta1");
            fileSystem.AddFile(Path.Combine(barRoot, "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"), "bar-sha");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg"), GetPackageStream(barPackage));

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(2, packages.Count);

            var package = packages[0];
            Assert.Equal("Foo", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0"), package.Version);
            var packageFile = Assert.Single(package.GetFiles());
            Assert.Equal(@"lib\net40\Foo.dll", packageFile.Path);
            Assert.Equal(".NETFramework,Version=v4.0", packageFile.TargetFramework.FullName);

            package = packages[1];
            Assert.Equal("Bar", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0-beta1"), package.Version);

            var files = package.GetFiles().OrderBy(p => p.Path.Length).ToList();
            Assert.Equal(3, files.Count);
            Assert.Equal(@"README.md", files[0].Path);

            packageFile = files[1];
            Assert.Equal(@"content\qwerty.js", packageFile.Path);
            Assert.Null(packageFile.TargetFramework);

            packageFile = files[2];
            Assert.Equal(@"lib\net451\test.dll", packageFile.Path);
            Assert.Equal(".NETFramework,Version=v4.5.1", packageFile.TargetFramework.FullName);
        }

        [Fact]
        public void GetPackages_SkipsPackagesWithoutHashFile()
        {
            // Arrange
            var barPackage = new PackageBuilder
            {
                Id = "Foo",
                Version = new SemanticVersion("1.0.0-beta1-345"),
                Description = "Some description",
            };
            barPackage.Authors.Add("test author");
            barPackage.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"lib\net45", "Foo.dll", "lib contents").Object);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine(Path.Combine("Foo", "1.0.0", "Foo.1.0.0.nupkg"), ""));
            var barRoot = Path.Combine("Bar", "1.0.0-beta1-345");
            fileSystem.AddFile(Path.Combine(barRoot, "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine(barRoot, "Bar.1.0.0-beta1-345.nupkg.sha512"));
            fileSystem.AddFile(Path.Combine(barRoot, "Bar.1.0.0-beta1-345.nupkg"), GetPackageStream(barPackage));

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            var package = Assert.Single(packages);

            Assert.Equal("Bar", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0-beta1-345"), package.Version);
        }

        [Fact]
        public void FindPackagesById_ReturnsAllVersionsOfAPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.1.0.0-beta1-345.nupkg.sha512"), "345sha");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-402</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.1.0.0-beta1-402.nupkg.sha512"), "402sha");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"), "beta1sha");

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.FindPackagesById("Bar").OrderBy(p => p.Version).ToList();

            // Assert
            Assert.Equal(3, packages.Count);
            Assert.Equal(new SemanticVersion("1.0.0-beta1"), packages[0].Version);
            Assert.Equal(new SemanticVersion("1.0.0-beta1-345"), packages[1].Version);
            Assert.Equal(new SemanticVersion("1.0.0-beta1-402"), packages[2].Version);
        }

        [Fact]
        public void FindPackagesById_IgnoresPackagesWithoutHashFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.1.0.0-beta1-345.nupkg.sha512"), "345sha");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.nuspec"));
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.1.0.0-beta1-402.nupkg"), "nupkg contents");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"), "beta1 hash");

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.FindPackagesById("Bar").OrderBy(p => p.Version).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal(new SemanticVersion("1.0.0-beta1"), packages[0].Version);
            Assert.Equal(new SemanticVersion("1.0.0-beta1-345"), packages[1].Version);
        }

        [Fact]
        public void FindPackageById_ReturnsSpecificVersionIfHashFileExists()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.1.0.0-beta1-345.nupkg.sha512"));

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>test-author</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"));

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var package = repository.FindPackage("Bar", new SemanticVersion("1.0.0.0-beta1"));

            // Assert
            Assert.NotNull(package);
            Assert.Equal(new SemanticVersion("1.0.0-beta1"), package.Version);
            var author = Assert.Single(package.Authors);
            Assert.Equal("test-author", author);
        }

        [Fact]
        public void FindPackageById_IgnoresVersionsWithoutHashFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nupkg"), @"Foo.nupkg contents");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>test-author</authors><description>None</description></metadata></package>");
            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var package = repository.FindPackage("Foo", new SemanticVersion("1.0.0"));

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void RemovePackage_DeletesPackageDirectory_IfItExists()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta2", "Foo.nuspec"), "Nuspec contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta2", "Foo.1.0.0-beta2.nupkg.sha512"), "hash contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta2", "tools", "net45", "Foo.targets"), "Foo.targets contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta4", "Foo.nuspec"), "1.0.0-beta4 Nuspec contents");

            var repository = new ExpandedPackageRepository(fileSystem);
            var package = PackageUtility.CreatePackage("Foo", "1.0-beta2");

            // Act
            repository.RemovePackage(package);

            // Assert
            var deletedItems = Assert.Single(fileSystem.Deleted);
            Assert.Contains(Path.Combine("Foo", "1.0.0-beta2"), deletedItems);
            Assert.True(fileSystem.FileExists(Path.Combine("Foo", "1.0.0-beta4", "Foo.nuspec")));
        }

        [Fact]
        public void RemovePackage_Succeeds_IfPackageDoesNotExist()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"), "Nuspec contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "tools", "net45", "Foo.targets"), "Foo.targets contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta4", "Foo.nupkg"), "1.0.0-beta4 Nuspec contents");

            var repository = new ExpandedPackageRepository(fileSystem);
            var package = PackageUtility.CreatePackage("Foo", "1.0.0-beta4");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.Empty(fileSystem.Deleted);
        }

        [Fact]
        public void AddPackage_AddsExpandedPackageToThePackageDirectory()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var repository = new ExpandedPackageRepository(fileSystem);
            var package = GetPackage();

            // Act
            repository.AddPackage(package);

            // Assert
            var reader = Manifest.ReadFrom(fileSystem.OpenFile(@"MyPackage\1.0.0-beta2\MyPackage.nuspec"), validateSchema: true);
            Assert.Equal("MyPackage", reader.Metadata.Id);
            Assert.Equal("1.0.0-beta2", reader.Metadata.Version);
            Assert.True(package.GetStream().ContentEquals(fileSystem.OpenFile(@"MyPackage\1.0.0-beta2\MyPackage.1.0.0-beta2.nupkg")));
            Assert.Equal(package.GetHash(new CryptoHashProvider()), fileSystem.ReadAllText(@"MyPackage\1.0.0-beta2\MyPackage.1.0.0-beta2.nupkg.sha512"));
        }

        private static IPackage GetPackage()
        {
            var packageBuilder = new PackageBuilder
            {
                Id = "MyPackage",
                Version = new SemanticVersion("1.0.0-beta2"),
                Description = "Test description",
            };
            packageBuilder.Authors.Add("test");

            packageBuilder.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"content\net40\App_Code", "PreapplicationStartCode.cs", content: "Preapplication content").Object);
            packageBuilder.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"tools\net40", "package.targets", "package.targets content").Object);
            packageBuilder.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"lib\net40", "MyPackage.dll", "lib contents").Object);

            return new ZipPackage(GetPackageStream(packageBuilder).ToStreamFactory(), enableCaching: false);
        }

        private static MemoryStream GetPackageStream(PackageBuilder packageBuilder)
        {
            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}

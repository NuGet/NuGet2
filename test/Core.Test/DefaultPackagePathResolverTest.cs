using System.IO;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class DefaultPackagePathResolverTest
    {
        [Fact]
        public void GetInstallPathPrependsFileSystemRootToPackageDirectory()
        {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string installPath = resolver.GetInstallPath(testPackage);

            // Assert
            Assert.Equal(Path.Combine(fs.Root, "Test.1.0"), installPath);
        }

        [Fact]
        public void GetPackageDirectoryWithSideBySideOnAppendsVersionToEndOfPackageDirectory()
        {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageDirectory(testPackage);

            // Assert
            Assert.Equal("Test.1.0", packageDir);
        }

        [Fact]
        public void GetPackageDirectoryWithSideBySideOffDoesNotAppendVersionToEndOfPackageDirectory()
        {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs, useSideBySidePaths: false);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageDirectory(testPackage);

            // Assert
            Assert.Equal("Test", packageDir);
        }

        [Fact]
        public void GetPackageFileNameWithSideBySideOnAppendsVersionToEndOfPackageDirectory()
        {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageFileName(testPackage);

            // Assert
            Assert.Equal("Test.1.0" + Constants.PackageExtension, packageDir);
        }

        [Fact]
        public void GetPackageFileNameWithSideBySideOffDoesNotAppendVersionToEndOfPackageDirectory()
        {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs, useSideBySidePaths: false);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageFileName(testPackage);

            // Assert
            Assert.Equal("Test" + Constants.PackageExtension, packageDir);
        }
    }
}

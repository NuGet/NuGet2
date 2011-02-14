using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class DefaultPackagePathResolverTest {
        [TestMethod]
        public void GetInstallPathPrependsFileSystemRootToPackageDirectory() {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string installPath = resolver.GetInstallPath(testPackage);

            // Assert
            Assert.AreEqual(fs.Root + "Test.1.0", installPath);
        }

        [TestMethod]
        public void GetPackageDirectoryWithSideBySideOnAppendsVersionToEndOfPackageDirectory() {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageDirectory(testPackage);

            // Assert
            Assert.AreEqual("Test.1.0", packageDir);
        }

        [TestMethod]
        public void GetPackageDirectoryWithSideBySideOffDoesNotAppendVersionToEndOfPackageDirectory() {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs, useSideBySidePaths: false);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageDirectory(testPackage);

            // Assert
            Assert.AreEqual("Test", packageDir);
        }

        [TestMethod]
        public void GetPackageFileNameWithSideBySideOnAppendsVersionToEndOfPackageDirectory() {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageFileName(testPackage);

            // Assert
            Assert.AreEqual("Test.1.0" + Constants.PackageExtension, packageDir);
        }

        [TestMethod]
        public void GetPackageFileNameWithSideBySideOffDoesNotAppendVersionToEndOfPackageDirectory() {
            // Arrange
            MockFileSystem fs = new MockFileSystem();
            DefaultPackagePathResolver resolver = new DefaultPackagePathResolver(fs, useSideBySidePaths: false);
            IPackage testPackage = PackageUtility.CreatePackage("Test");

            // Act
            string packageDir = resolver.GetPackageFileName(testPackage);

            // Assert
            Assert.AreEqual("Test" + Constants.PackageExtension, packageDir);
        }
    }
}

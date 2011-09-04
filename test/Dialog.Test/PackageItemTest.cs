using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.Dialog.Test {


    public class PackageItemTest {
		
        [Fact]
        public void PackageIdentityPropertyReturnsCorrectObject() {

            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Same(package, packageItem.PackageIdentity);
        }

        [Fact]
        public void PropertyNameIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal("A", packageItem.Name);
            Assert.Equal("A", packageItem.Id);
        }

        [Fact]
        public void PropertyVersionIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal("1.0", packageItem.Version);
        }

        [Fact]
        public void PropertyIsEnabledIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal(true, packageItem.IsEnabled);
        }

        [Fact]
        public void PropertyDescriptionIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal(package.Description, packageItem.Description);
        }

        [Fact]
        public void PropertyAuthorsIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act
            IList<string> authors = packageItem.Authors.ToList();

            // Act && Assert 
            Assert.Equal(1, authors.Count);
            Assert.Equal("Tester", authors[0]);
        }

        [Fact]
        public void PropertyLicenseUrlIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act
            Uri licenseUrl = packageItem.LicenseUrl;

            // Act && Assert 
            Assert.Equal("ftp://test/somelicense.txts", licenseUrl.AbsoluteUri);
        }

        private static PackageItem CreatePackageItem(IPackage package) {
            var packageManager = new Mock<IVsPackageManager>();
            var localRepository = new Mock<IPackageRepository>();

            var provider = new MockPackagesProvider(localRepository.Object, packageManager.Object);
            return new PackageItem(provider, package);
        }
    }
}

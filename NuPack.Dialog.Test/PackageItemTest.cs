using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test {

    [TestClass]
    public class PackageItemTest {


        [TestMethod]
        public void PackageIdentityPropertyReturnsCorrectObject() {

            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.AreSame(package, packageItem.PackageIdentity);
        }

        [TestMethod]
        public void PropertyNameIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.AreEqual("A", packageItem.Name);
            Assert.AreEqual("A", packageItem.Id);
        }

        [TestMethod]
        public void PropertyVersionIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.AreEqual("1.0", packageItem.Version);
        }

        [TestMethod]
        public void PropertyIsEnabledIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.AreEqual(true, packageItem.IsEnabled);
        }

        [TestMethod]
        public void PropertyDescriptionIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.AreEqual(package.Description, packageItem.Description);
        }

        [TestMethod]
        public void PropertyAuthorsIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act
            IList<string> authors = packageItem.Authors.ToList();

            // Act && Assert 
            Assert.AreEqual(1, authors.Count);
            Assert.AreEqual("Tester", authors[0]);
        }

        [TestMethod]
        public void PropertyLicenseUrlIsCorrect() {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act
            Uri licenseUrl = packageItem.LicenseUrl;

            // Act && Assert 
            Assert.AreEqual("ftp://test/somelicense.txts", licenseUrl.AbsoluteUri);
        }

        private static PackageItem CreatePackageItem(IPackage package) {
            var packageManager = new Mock<IVsPackageManager>();
            var projectManager = new Mock<IProjectManager>();

            MockPackagesProvider provider = new MockPackagesProvider(packageManager.Object, projectManager.Object);
            return new PackageItem(provider, package, null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.Dialog.Test
{
    public class PackageItemTest
    {
        [Fact]
        public void PackageIdentityPropertyReturnsCorrectObject()
        {

            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Same(package, packageItem.PackageIdentity);
        }

        [Fact]
        public void PropertyNameIsCorrect()
        {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal("A", packageItem.Name);
            Assert.Equal("A", packageItem.Id);
        }

        [Fact]
        public void PropertyVersionIsCorrect()
        {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal("1.0", packageItem.Version);
        }

        [Fact]
        public void PropertyIsEnabledIsCorrect()
        {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal(true, packageItem.IsEnabled);
        }

        [Fact]
        public void PropertyDescriptionIsCorrect()
        {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act && Assert 
            Assert.Equal(package.Description, packageItem.Description);
        }

        [Fact]
        public void PropertyAuthorsIsCorrect()
        {
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
        public void PropertyLicenseUrlIsCorrect()
        {
            // Arrange
            IPackage package = PackageUtility.CreatePackage("A", "1.0", new string[] { "This is a package." });
            var packageItem = CreatePackageItem(package);

            // Act
            Uri licenseUrl = packageItem.LicenseUrl;

            // Act && Assert 
            Assert.Equal("ftp://test/somelicense.txts", licenseUrl.AbsoluteUri);
        }

        [Fact]
        public void DependenciesAreFilteredByTargetFramework1()
        {
            // Arrange
            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=2.0"),
                    new [] { new PackageDependency("A"), new PackageDependency("B") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=3.0"),
                    new [] { new PackageDependency("B"), new PackageDependency("C") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("C"), new PackageDependency("D") }
                ),
            };

            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns("P");
            package.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(p => p.DependencySets).Returns(dependencySets);

            var item = CreatePackageItem(package.Object);
            item.TargetFramework = new FrameworkName(".NETFramework, Version=2.0");

            // Act
            var dependencies = item.Dependencies.ToList();

            // Assert
            Assert.Equal(2, dependencies.Count);
            Assert.Equal("A", dependencies[0].Id);
            Assert.Equal("B", dependencies[1].Id);
        }

        [Fact]
        public void DependenciesAreFilteredByTargetFramework2()
        {
            // Arrange
            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=2.0"),
                    new [] { new PackageDependency("A"), new PackageDependency("B") }
                ),
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=3.0"),
                    new [] { new PackageDependency("B"), new PackageDependency("C") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("C"), new PackageDependency("D") }
                ),
            };

            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns("P");
            package.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(p => p.DependencySets).Returns(dependencySets);

            var item = CreatePackageItem(package.Object);
            item.TargetFramework = new FrameworkName("Silverlight, Version=4.5");

            // Act
            var dependencies = item.Dependencies.ToList();

            // Assert
            Assert.Equal(2, dependencies.Count);
            Assert.Equal("B", dependencies[0].Id);
            Assert.Equal("C", dependencies[1].Id);
        }

        [Fact]
        public void DependenciesAreFilteredByTargetFramework3()
        {
            // Arrange
            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=2.0"),
                    new [] { new PackageDependency("A"), new PackageDependency("B") }
                ),
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=3.0"),
                    new [] { new PackageDependency("B"), new PackageDependency("C") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("C"), new PackageDependency("D") }
                ),
            };

            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns("P");
            package.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(p => p.DependencySets).Returns(dependencySets);

            var item = CreatePackageItem(package.Object);
            item.TargetFramework = new FrameworkName("Silverlight, Version=1.0");

            // Act
            var dependencies = item.Dependencies.ToList();

            // Assert
            Assert.Equal(0, dependencies.Count);
        }

        [Fact]
        public void DependenciesAreFilteredByTargetFramework4()
        {
            // Arrange
            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=2.0"),
                    new [] { new PackageDependency("A"), new PackageDependency("B") }
                ),
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=3.0"),
                    new [] { new PackageDependency("B"), new PackageDependency("C") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("C"), new PackageDependency("D") }
                ),
            };

            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns("P");
            package.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(p => p.DependencySets).Returns(dependencySets);

            var item = CreatePackageItem(package.Object);
            item.TargetFramework = new FrameworkName("Silverlight, Version=1.0");

            // Act
            var dependencies = item.Dependencies.ToList();

            // Assert
            Assert.Equal(0, dependencies.Count);
        }

        [Fact]
        public void PackageItemReturnsAllDependenciesWhenTargetFrameworkIsNull()
        {
            // Arrange
            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=2.0"),
                    new [] { new PackageDependency("A"), new PackageDependency("B") }
                ),
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=3.0"),
                    new [] { new PackageDependency("C"), new PackageDependency("D") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new [] { new PackageDependency("E"), new PackageDependency("F") }
                ),
            };

            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns("P");
            package.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(p => p.DependencySets).Returns(dependencySets);

            var item = CreatePackageItem(package.Object);
            item.TargetFramework = null;

            // Act
            var dependencies = item.Dependencies.ToList();

            // Assert
            Assert.Equal(6, dependencies.Count);
            Assert.Equal("A", dependencies[0].Id);
            Assert.Equal("B", dependencies[1].Id);
            Assert.Equal("C", dependencies[2].Id);
            Assert.Equal("D", dependencies[3].Id);
            Assert.Equal("E", dependencies[4].Id);
            Assert.Equal("F", dependencies[5].Id);
        }

        [Fact]
        public void DependenciesAreFilteredByTargetFramework6()
        {
            // Arrange
            var dependencySets = new PackageDependencySet[] {
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=2.0"),
                    new [] { new PackageDependency("A"), new PackageDependency("B") }
                ),
                new PackageDependencySet(
                    new FrameworkName("Silverlight, Version=3.0"),
                    new [] { new PackageDependency("B"), new PackageDependency("C") }
                ),
                new PackageDependencySet(
                    new FrameworkName(".NETFramework, Version=4.0"),
                    new PackageDependency[0]
                ),
            };

            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns("P");
            package.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(p => p.DependencySets).Returns(dependencySets);

            var item = CreatePackageItem(package.Object);
            item.TargetFramework = new FrameworkName(".NETFramework, Version=4.0");

            // Act
            var dependencies = item.Dependencies.ToList();

            // Assert
            Assert.Equal(0, dependencies.Count);
        }

        private static PackageItem CreatePackageItem(IPackage package)
        {
            var packageManager = new Mock<IVsPackageManager>();
            var localRepository = new Mock<IPackageRepository>();

            var provider = new MockPackagesProvider(localRepository.Object, packageManager.Object);
            return new PackageItem(provider, package);
        }
    }
}

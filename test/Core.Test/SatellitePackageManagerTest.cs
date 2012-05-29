using System.Collections.Generic;
using System.Linq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class SatellitePackageManagerTest
    {
        [Fact]
        public void IsSatellitePackageReturnsFalseForNullLanguage()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo");
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackages);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseWhenMissingLanguageSuffix()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo", language: "fr-fr");
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackages);
        }

        [Fact]
        public void IsSatellitePackageHandlesBadPackageId()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage(".fr-fr", language: "fr-fr");
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackages);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseWhenMissingDependency()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr");
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackages);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseWhenRuntimePackageNotInRepository()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr", dependencies: new[] { new PackageDependency("foo") });
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.False(isSatellite);
            Assert.True(!runtimePackages.Any());
        }

        [Fact]
        public void IsSatellitePackageReturnsTrueWhenRuntimePackageIdentified()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var runtime = PackageUtility.CreatePackage("foo");
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr", dependencies: new[] { new PackageDependency("foo") });
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));
            repository.AddPackage(runtime);

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.True(isSatellite);
            Assert.NotNull(runtimePackages);
        }

        [Fact]
        public void IsSatellitePackageReturnsEachRuntimePackageThatSatisfiesDependencyConstraints()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var runtime10 = PackageUtility.CreatePackage("foo", "1.0");
            var runtime12 = PackageUtility.CreatePackage("foo", "1.2");
            var runtime14alpha = PackageUtility.CreatePackage("foo", "1.4-alpha");
            var runtime20 = PackageUtility.CreatePackage("foo", "2.0");
            var bar20 = PackageUtility.CreatePackage("bar", "1.0");
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr",
                                            dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0, 2.0)")) });
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));
            repository.AddPackage(runtime10);
            repository.AddPackage(runtime12);
            repository.AddPackage(runtime14alpha);
            repository.AddPackage(runtime20);
            repository.AddPackage(bar20);

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            var packageList = runtimePackages.ToList();
            Assert.True(isSatellite);
            Assert.Equal(3, packageList.Count);
            Assert.Contains(runtime10, packageList);
            Assert.Contains(runtime12, packageList);
            Assert.Contains(runtime14alpha, packageList);
        }

        [Fact]
        public void IsSatellitePackageIgnoresCaseOnLanguage()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var runtime = PackageUtility.CreatePackage("foo");
            var package = PackageUtility.CreatePackage("foo.Fr-Fr", language: "fr-FR", dependencies: new[] { new PackageDependency("foo") });
            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            repository.AddPackage(runtime);

            // Act
            IEnumerable<IPackage> runtimePackages;
            var isSatellite = satellitePackageManager.IsSatellitePackage(package, out runtimePackages);

            // Assert
            Assert.True(isSatellite);
            Assert.NotNull(runtimePackages);
        }

        [Fact]
        public void ExpandSatellitePackageAddsSatelliteFilesToDirectory()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var corePackage = PackageUtility.CreatePackage("Main", version: "1.0", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var langPackage = PackageUtility.CreatePackage("Main.sr-Cyrl-RS", version: "1.0",
                                    language: "sr-Cyrl-RS",
                                    description: "Localized",
                                    assemblyReferences: new[] { @"lib\net40\sr-Cyrl-RS\Main.resources.dll" },
                                    dependencies: new[] { new PackageDependency("Main") });
            repository.AddPackage(corePackage);

            var fileSystem = new MockFileSystem();
            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            bool expanded = satellitePackageManager.ExpandSatellitePackage(langPackage);

            // Assert
            Assert.True(expanded);
            Assert.Equal(2, fileSystem.Paths.Count);
            Assert.Contains(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref", fileSystem.Paths.Keys);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll", fileSystem.Paths.Keys);
        }

        [Fact]
        public void RemoveSatelliteReferenceRemovesSatelliteFiles()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var corePackage = PackageUtility.CreatePackage("Main", version: "1.0", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var langPackage = PackageUtility.CreatePackage("Main.sr-Cyrl-RS", version: "1.0",
                                    language: "sr-Cyrl-RS",
                                    description: "Localized",
                                    assemblyReferences: new[] { @"lib\net40\sr-Cyrl-RS\Main.resources.dll" },
                                    dependencies: new[] { new PackageDependency("Main") });
            repository.AddPackage(corePackage);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll");

            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            bool removed = satellitePackageManager.RemoveSatelliteReferences(langPackage);

            // Assert
            Assert.True(removed);
            Assert.Equal(4, fileSystem.Deleted.Count);
            Assert.Contains(@"Main.1.0\.satellite", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll", fileSystem.Deleted);
        }

        [Fact]
        public void RemoveSatelliteReferenceRemovesSatelliteFilesFromAllCorePackages()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var corePackage10 = PackageUtility.CreatePackage("Main", version: "1.0", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var corePackage12 = PackageUtility.CreatePackage("Main", version: "1.2", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var langPackage = PackageUtility.CreatePackage("Main.sr-Cyrl-RS", version: "1.0",
                                    language: "sr-Cyrl-RS",
                                    description: "Localized",
                                    assemblyReferences: new[] { @"lib\net40\sr-Cyrl-RS\Main.resources.dll" },
                                    dependencies: new[] { new PackageDependency("Main") });
            repository.AddPackage(corePackage10);
            repository.AddPackage(corePackage12);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll");
            fileSystem.AddFile(@"Main.1.2\.satellite\Main.sr-Cyrl-RS$1.0.ref");
            fileSystem.AddFile(@"Main.1.2\lib\net40\sr-Cyrl-RS\Main.resources.dll");

            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            bool removed = satellitePackageManager.RemoveSatelliteReferences(langPackage);

            // Assert
            Assert.True(removed);
            Assert.Equal(8, fileSystem.Deleted.Count);
            Assert.Contains(@"Main.1.0\.satellite", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll", fileSystem.Deleted);

            Assert.Contains(@"Main.1.2\.satellite", fileSystem.Deleted);
            Assert.Contains(@"Main.1.2\.satellite\Main.sr-Cyrl-RS$1.0.ref", fileSystem.Deleted);
            Assert.Contains(@"Main.1.2\lib\net40\sr-Cyrl-RS", fileSystem.Deleted);
            Assert.Contains(@"Main.1.2\lib\net40\sr-Cyrl-RS\Main.resources.dll", fileSystem.Deleted);
        }

        [Fact]
        public void RemoveSatelliteReferenceRemovesSatelliteFilesFromCorePackage()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var corePackage = PackageUtility.CreatePackage("Main", version: "1.0", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var langPackage1 = PackageUtility.CreatePackage("Main.sr-Cyrl-RS", version: "1.0",
                                    language: "sr-Cyrl-RS",
                                    description: "Localized",
                                    assemblyReferences: new[] { @"lib\net40\sr-Cyrl-RS\Main.resources.dll" },
                                    dependencies: new[] { new PackageDependency("Main") });
            var langPackage2 = PackageUtility.CreatePackage("Main.fr-FR", version: "1.0",
                                    language: "fr-FR",
                                    description: "Localized",
                                    assemblyReferences: new[] { @"lib\net40\fr-FR\Main.resources.dll" },
                                    dependencies: new[] { new PackageDependency("Main") });
            repository.AddPackage(corePackage);
            repository.AddPackage(langPackage1);
            repository.AddPackage(langPackage2);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.fr-FR$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll");
            fileSystem.AddFile(@"Main.1.0\lib\net40\fr-FR\Main.resources.dll");

            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            bool removed = satellitePackageManager.RemoveSatelliteReferences(corePackage);

            // Assert
            Assert.True(removed);
            Assert.Equal(7, fileSystem.Deleted.Count);
            Assert.Contains(@"Main.1.0\.satellite", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\.satellite\Main.fr-FR$1.0.ref", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\fr-FR", fileSystem.Deleted);
            Assert.Contains(@"Main.1.0\lib\net40\fr-FR\Main.resources.dll", fileSystem.Deleted);
        }

        [Fact]
        public void GetSatelliteReferencesReturnsReferencesForEachRefInSatelliteDirectory()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var corePackage = PackageUtility.CreatePackage("Main", version: "1.0", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var langPackage1 = PackageUtility.CreatePackage("Main.sr-Cyrl-RS", version: "1.0",
                                    language: "sr-Cyrl-RS",
                                    description: "Localized",
                                    dependencies: new[] { new PackageDependency("Main") });
            var langPackage2 = PackageUtility.CreatePackage("Main.fr-FR", version: "1.0",
                                    language: "fr-FR",
                                    description: "Localized",
                                    dependencies: new[] { new PackageDependency("Main") });
            repository.AddPackage(corePackage);
            repository.AddPackage(langPackage1);
            repository.AddPackage(langPackage2);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.fr-FR$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll");

            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            var references = satellitePackageManager.GetSatelliteReferences(corePackage).ToList();

            // Assert
            Assert.Equal(2, references.Count);
            Assert.Contains(langPackage1, references);
            Assert.Contains(langPackage2, references);
        }

        [Fact]
        public void GetSatelliteReferencesIgnoresRefFilesWithNoCorrespondingPackage()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var corePackage = PackageUtility.CreatePackage("Main", version: "1.0", description: "Test", assemblyReferences: new[] { @"lib\net40\Main.dll" });
            var langPackage1 = PackageUtility.CreatePackage("Main.sr-Cyrl-RS", version: "1.0",
                                    language: "sr-Cyrl-RS",
                                    description: "Localized",
                                    dependencies: new[] { new PackageDependency("Main") });
            var langPackage2 = PackageUtility.CreatePackage("Main.fr-FR", version: "1.0",
                                    language: "fr-FR",
                                    description: "Localized",
                                    dependencies: new[] { new PackageDependency("Main") });
            repository.AddPackage(corePackage);
            repository.AddPackage(langPackage1);
            repository.AddPackage(langPackage2);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.sr-Cyrl-RS$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.fr-FR$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\.satellite\Main.se$1.0.ref");
            fileSystem.AddFile(@"Main.1.0\lib\net40\sr-Cyrl-RS\Main.resources.dll");

            var satellitePackageManager = new SatellitePackageManager(repository, fileSystem, new DefaultPackagePathResolver(fileSystem));

            // Act
            var references = satellitePackageManager.GetSatelliteReferences(corePackage).ToList();

            // Assert
            Assert.Equal(2, references.Count);
            Assert.Contains(langPackage1, references);
            Assert.Contains(langPackage2, references);
        }
    }
}

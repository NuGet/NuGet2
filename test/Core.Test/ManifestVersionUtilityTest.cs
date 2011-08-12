using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class ManifestVersionUtilityTest {
        [TestMethod]
        public void GetManifestVersionReturns1IfNoNewPropertiesAreSet() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);
           
            // Assert
            Assert.AreEqual(1, version);
        }

        [TestMethod]
        public void GetManifestVersionReturns1IfFrameworkAssemblyHasValues() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                FrameworkAssemblies = new List<ManifestFrameworkAssembly> {  
                    new ManifestFrameworkAssembly { AssemblyName = "System.Data.dll" }
                }
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.AreEqual(1, version);
        }

        [TestMethod]
        public void GetManifestVersionReturns2IfCopyrightIsSet() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                Copyright = "© Outercurve Foundation"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.AreEqual(2, version);
        }

        [TestMethod]
        public void GetManifestVersionReturns2IfFrameworkAssemblyAndReferencesAreSet() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                FrameworkAssemblies = new List<ManifestFrameworkAssembly> {  
                    new ManifestFrameworkAssembly { AssemblyName = "System.Data.dll" }
                },
                References = new List<ManifestReference> {
                    new ManifestReference { File = "Foo.dll" }
                }
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.AreEqual(2, version);
        }

        [TestMethod]
        public void GetManifestVersionConsidersEmptyLists() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                FrameworkAssemblies = new List<ManifestFrameworkAssembly> {  
                },
                References = new List<ManifestReference> {
                }
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.AreEqual(2, version);
        }

        [TestMethod]
        public void GetManifestVersionReturns2IfReleaseNotesIsPresent() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                ReleaseNotes = "Notes.txt"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.AreEqual(2, version);
        }

        [TestMethod]
        public void GetManifestVersionIgnoresEmptyStrings() {
            // Arrange
            var metadata = new ManifestMetadata {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                ReleaseNotes = ""
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.AreEqual(1, version);
        }

    }
}

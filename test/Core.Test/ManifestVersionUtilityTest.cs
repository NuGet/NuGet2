using System.Collections.Generic;
using Xunit;

namespace NuGet.Test
{
    public class ManifestVersionUtilityTest
    {
        [Fact]
        public void GetManifestVersionReturns1IfNoNewPropertiesAreSet()
        {
            // Arrange
            var metadata =    new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(1, version);
        }

        [Fact]
        public void GetManifestVersionReturns1IfFrameworkAssemblyHasValues()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
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
            Assert.Equal(1, version);
        }

        [Fact]
        public void GetManifestVersionReturns2IfCopyrightIsSet()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                Copyright = "© .NET Foundation"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(2, version);
        }

        [Fact]
        public void GetManifestVersionReturns2IfFrameworkAssemblyAndReferencesAreSet()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                FrameworkAssemblies = new List<ManifestFrameworkAssembly> {
                    new ManifestFrameworkAssembly { AssemblyName = "System.Data.dll" }
                },
                ReferenceSets = new List<ManifestReferenceSet> {
                    new ManifestReferenceSet
                    {
                        References = new List<ManifestReference> {
                            new ManifestReference { File = "Foo.dll" }
                        }
                    }
                }
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(2, version);
        }

        [Fact]
        public void GetManifestVersionConsidersEmptyLists()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                FrameworkAssemblies = new List<ManifestFrameworkAssembly>
                {
                },
                ReferenceSets = new List<ManifestReferenceSet>
                {
                }
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(1, version);
        }

        [Fact]
        public void GetManifestVersionReturns2IfReleaseNotesIsPresent()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                ReleaseNotes = "Notes.txt"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(2, version);
        }

        [Fact]
        public void GetManifestVersionIgnoresEmptyStrings()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0",
                Authors = "A, B",
                Description = "Description",
                ReleaseNotes = ""
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(1, version);
        }

        [Fact]
        public void GetManifestVersionReturns3IfUsingSemanticVersioning()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0.0-alpha",
                Authors = "A, B",
                Description = "Description"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(3, version);
        }
    }
}

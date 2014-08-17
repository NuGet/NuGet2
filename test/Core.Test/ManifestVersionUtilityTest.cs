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
            var metadata = new ManifestMetadata
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
                Copyright = "© Outercurve Foundation"
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

        [Fact]
        public void GetManifestVersionReturnsV7IfUsingLicenseName()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "Foo",
                Version = "1.0.0",
                LicenseNames = "Apache 2"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(7, version);
        }

        [Fact]
        public void GetManifestVersionReturnsV7IfUsingRepositoryUrl()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "NuGetGallery",
                Version = "1.0.0",
                RepositoryUrl = "http://github.com/nuget/nugetgallery.git"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(7, version);
        }

        [Fact]
        public void GetManifestVersionReturnsV7IfUsingRepositoryType()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "NuGetGallery",
                Version = "1.0.0",
                RepositoryType = "git"
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(7, version);
        }

        [Fact]
        public void GetManifestVersionReturnsV7IfUsingProperties()
        {
            // Arrange
            var metadata = new ManifestMetadata
            {
                Id = "NuGet.Core",
                Version = "1.0.0",
                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "propertyName" } }
            };

            // Act
            var version = ManifestVersionUtility.GetManifestVersion(metadata);

            // Assert
            Assert.Equal(7, version);
        }
    }
}

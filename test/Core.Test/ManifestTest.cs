using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class ManifestTest
    {
        [Fact]
        public void ManifestValidatesMetadata()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = String.Empty,
                    Version = String.Empty,
                    Authors = String.Empty,
                    Description = null
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest),
                "Id is required.\r\nVersion is required.\r\nAuthors is required.\r\nDescription is required.");
        }

        [Fact]
        public void ManifestValidatesMetadataUrlsIfEmpty()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    IconUrl = "",
                    LicenseUrl = "",
                    ProjectUrl = ""
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest),
                "LicenseUrl cannot be empty.\r\nIconUrl cannot be empty.\r\nProjectUrl cannot be empty.");
        }

        [Fact]
        public void ManifestValidatesManifestFiles()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                },
                Files = new List<ManifestFile> {
                    new ManifestFile { 
                        Source = "|",
                        Target = "<"
                    },
                    new ManifestFile { 
                        Source = @"foo\bar\|>",
                        Target = "lib"
                    },
                    new ManifestFile {
                        Source = @"foo\**\*.cs",
                        Exclude = "Exclude|"
                    }
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest),
                "Source path '|' contains invalid characters.\r\nTarget path '<' contains invalid characters.\r\nSource path 'foo\\bar\\|>' contains invalid characters.\r\nExclude path 'Exclude|' contains invalid characters.");
        }

        [Fact]
        public void ManifestEnsuresManifestReferencesDoNotContainInvalidCharacters()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    ReferenceSets = new List<ManifestReferenceSet> {
                        new ManifestReferenceSet
                        {
                            References = new List<ManifestReference> {
                                new ManifestReference { File = "Foo?.dll" },
                                new ManifestReference { File = "Bar*.dll" },
                                new ManifestReference { File = @"net40\baz.dll" }
                            }
                        },
                        new ManifestReferenceSet
                        {
                            TargetFramework = ".NETFramework, Version=4.0",
                            References = new List<ManifestReference> {
                                new ManifestReference { File = "wee?dd.dll" }
                            }
                        }
                    }
                },
                Files = new List<ManifestFile> {
                    new ManifestFile { Source = "Foo.dll", Target = "lib" }
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest),
                "Assembly reference 'Foo?.dll' contains invalid characters.\r\nAssembly reference 'Bar*.dll' contains invalid characters.\r\nAssembly reference 'net40\\baz.dll' contains invalid characters.\r\nAssembly reference 'wee?dd.dll' contains invalid characters.");
        }

        [Fact]
        public void ManifestValidatesDependencies()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    DependencySets = new List<ManifestDependencySet> {
                            new ManifestDependencySet {
                                TargetFramework = null,
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = null }
                                    }
                            }
                    }
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest), "Dependency Id is required.");
        }

        [Fact]
        public void ReadFromReadsRequiredValues()
        {
            // Arrange
            var manifestStream = CreateManifest();
            var expectedManifest = new Manifest
            {
                Metadata = new ManifestMetadata { Id = "Test-Pack", Version = "1.0.0", Description = "Test description", Authors = "NuGet Test" }
            };

            // Act 
            var manifest = Manifest.ReadFrom(manifestStream, validateSchema: true);

            // Assert
            AssertManifest(expectedManifest, manifest);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("1")]
        [InlineData("1.0.2.4.2")]
        [InlineData("abc.2")]
        [InlineData("1.2-alpha")]
        public void InvalidReuiredMinVersionValueWillThrow(string minVersionValue)
        {
            // Arrange
            var manifestStream = CreateManifest(minClientVersion: minVersionValue);

            // Act && Assert
            ExceptionAssert.Throws<InvalidDataException>(
                () => Manifest.ReadFrom(manifestStream, validateSchema: true),
                "The 'minClientVersion' attribute in the package manifest has invalid value. It must be a valid version string.");
        }

        [Fact]
        public void EmptyReuiredMinVersionValueWillNotThrow()
        {
            // Arrange
            var manifestStream = CreateManifest(minClientVersion: "");

            // Act
            var manifest = Manifest.ReadFrom(manifestStream, validateSchema: true);

            // Assert
            Assert.Null(manifest.Metadata.MinClientVersion);
        }

        [Fact]
        public void ReadFromReadsAllMetadataValues()
        {
            var references = new List<ManifestReferenceSet>
            {
                new ManifestReferenceSet {
                    TargetFramework = null,
                    References = new List<ManifestReference> 
                    {
                        new ManifestReference { File = "Test.dll" },
                    }
                },
                new ManifestReferenceSet {
                    TargetFramework = "hello",
                    References = new List<ManifestReference> 
                    {
                        new ManifestReference { File = "world.winmd" },
                    }
                }
            };

            // Arrange
            var manifestStream = CreateManifest(id: "Test-Pack2", version: "1.0.0-alpha", title: "blah", authors: "Outercurve",
                licenseUrl: "http://nuget.org/license", licenseNames: "Apache 2", projectUrl: "http://nuget.org/project", iconUrl: "https://nuget.org/icon",
                repositoryType: "git", repositoryUrl: "http://github.com/nuget/nugetgallery.git",
                requiresLicenseAcceptance: true, developmentDependency: true, description: "This is not a description",
                summary: "This is a summary", releaseNotes: "Release notes",
                copyright: "Copyright 2012", language: "fr-FR", tags: "Test Unit",
                properties: new[] { new ManifestProperty { Name = "customProperty", Value = "customPropertyValue" } },
                dependencies: new[] { new ManifestDependency { Id = "Test", Version = "1.2.0" } },
                assemblyReference: new[] { new ManifestFrameworkAssembly { AssemblyName = "System.Data", TargetFramework = "4.0" } },
                references: references,
                minClientVersion: "2.0.1.0"
            );

            var expectedManifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Test-Pack2",
                    Version = "1.0.0-alpha",
                    Description = "This is not a description",
                    Authors = "Outercurve",
                    LicenseUrl = "http://nuget.org/license",
                    LicenseNames = "Apache 2",
                    ProjectUrl = "http://nuget.org/project",
                    RepositoryUrl = "http://github.com/nuget/nugetgallery.git",
                    RepositoryType = "git",
                    IconUrl = "https://nuget.org/icon",
                    RequireLicenseAcceptance = true,
                    DevelopmentDependency = true,
                    Summary = "This is a summary",
                    ReleaseNotes = "Release notes",
                    Copyright = "Copyright 2012",
                    Language = "fr-FR",
                    Tags = "Test Unit",
                    Properties = new List<ManifestProperty> {
                        new ManifestProperty { Name = "customProperty", Value = "customPropertyValue" }
                    },
                    DependencySets = new List<ManifestDependencySet> {
                            new ManifestDependencySet {
                                TargetFramework = null,
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "Test", Version = "1.2.0" }
                                    }
                            }
                    },
                    FrameworkAssemblies = new List<ManifestFrameworkAssembly> { new ManifestFrameworkAssembly { AssemblyName = "System.Data", TargetFramework = "4.0" } },
                    ReferenceSets = new List<ManifestReferenceSet>
                    {
                        new ManifestReferenceSet {
                            TargetFramework = null,
                            References = new List<ManifestReference> 
                            {
                                new ManifestReference { File = "Test.dll" },
                            }
                        },
                        new ManifestReferenceSet {
                            TargetFramework = "hello",
                            References = new List<ManifestReference> 
                            {
                                new ManifestReference { File = "world.winmd" },
                            }
                        }
                    },
                    MinClientVersionString = "2.0.1.0"
                }
            };

            // Act 
            var manifest = Manifest.ReadFrom(manifestStream, validateSchema: true);

            // Assert
            AssertManifest(expectedManifest, manifest);
        }

        [Fact]
        public void ReadFromReadsFilesAndExpandsDelimitedFileList()
        {
            // Arrange
            var manifestStream = CreateManifest(files: new[] { 
                    new ManifestFile { Source = "Foo.cs", Target = "src" }, 
                    new ManifestFile { Source = @"**\bin\*.dll;**\bin\*.exe", Target = @"lib\net40", Exclude = @"**\*Test*" }
            });

            var expectedManifest = new Manifest
            {
                Metadata = new ManifestMetadata { Id = "Test-Pack", Version = "1.0.0", Description = "Test description", Authors = "NuGet Test" },
                Files = new List<ManifestFile> { 
                    new ManifestFile { Source = "Foo.cs", Target = "src" }, 
                    new ManifestFile { Source = @"**\bin\*.dll", Target = @"lib\net40", Exclude = @"**\*Test*" },
                    new ManifestFile { Source = @"**\bin\*.exe", Target = @"lib\net40", Exclude = @"**\*Test*" },
                }
            };

            // Act 
            var manifest = Manifest.ReadFrom(manifestStream, validateSchema: true);

            // Assert
            AssertManifest(expectedManifest, manifest);
        }

        [Fact]
        public void ReadFromDoesNotThrowIfValidateSchemaIsFalse()
        {
            // Arrange
            string content = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"">
  <metadata hello=""world"">
    <id>A</id>
    <version>1.0</version>
    <authors>Luan</authors>
    <owners>Luan</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Descriptions</description>
    
    <extra>This element is not defined in schema.</extra>
  </metadata>
  <clark>meko</clark>
  <files>
      <file src=""my.txt"" destination=""outdir"" />
  </files>
</package>";
            // Act
            var manifest = Manifest.ReadFrom(content.AsStream(), validateSchema: false);

            // Assert
            Assert.Equal("A", manifest.Metadata.Id);
            Assert.Equal("1.0", manifest.Metadata.Version);
            Assert.Equal("Luan", manifest.Metadata.Authors);
            Assert.False(manifest.Metadata.RequireLicenseAcceptance);
            Assert.False(manifest.Metadata.DevelopmentDependency);
            Assert.Equal("Descriptions", manifest.Metadata.Description);
        }

        [Fact]
        public void ReadDevelopmentDependency()
        {
            // Arrange
            string content = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"">
  <metadata hello=""world"">
    <id>A</id>
    <version>1.0</version>
    <authors>Luan</authors>
    <owners>Luan</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <developmentDependency>true</developmentDependency>
    <description>Descriptions</description>
    
    <extra>This element is not defined in schema.</extra>
  </metadata>
  <clark>meko</clark>
  <files>
      <file src=""my.txt"" destination=""outdir"" />
  </files>
</package>";
            // Act
            var manifest = Manifest.ReadFrom(content.AsStream(), validateSchema: false);

            // Assert
            Assert.Equal("A", manifest.Metadata.Id);
            Assert.Equal("1.0", manifest.Metadata.Version);
            Assert.Equal("Luan", manifest.Metadata.Authors);
            Assert.False(manifest.Metadata.RequireLicenseAcceptance);
            Assert.True(manifest.Metadata.DevelopmentDependency);
            Assert.Equal("Descriptions", manifest.Metadata.Description);
        }

        [Fact]
        public void ReadFromThrowIfValidateSchemaIsTrue()
        {
            // Arrange
            string content = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"">
  <metadata hello=""world"">
    <id>A</id>
    <version>1.0</version>
    <authors>Luan</authors>
    <owners>Luan</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Descriptions</description>
  </metadata>
</package>";

            // Switch to invariant culture to ensure the error message is in english.
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            // Act && Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => Manifest.ReadFrom(content.AsStream(), validateSchema: true),
                "The 'hello' attribute is not declared.");
        }

        [Fact]
        public void ManifestGroupDependencySetsByTargetFrameworkAndPutNullFrameworkFirst()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    DependencySets = new List<ManifestDependencySet> {
                            new ManifestDependencySet {
                                TargetFramework = ".NETFramework40",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "B" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = null,
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "A" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = null,
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "C" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = "Silverlight35",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "D" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = "net40",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "E" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = "sl35",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "F" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = "winrt45",
                                Dependencies = new List<ManifestDependency>() 
                            },
                    }
                }
            };

            // Act
            var dependencySets = ((IPackageMetadata)manifest.Metadata).DependencySets.ToList();

            // Assert
            Assert.Equal(4, dependencySets.Count);

            Assert.Null(dependencySets[0].TargetFramework);
            Assert.Equal(2, dependencySets[0].Dependencies.Count);
            Assert.Equal("A", dependencySets[0].Dependencies.First().Id);
            Assert.Equal("C", dependencySets[0].Dependencies.Last().Id);

            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), dependencySets[1].TargetFramework);
            Assert.Equal(0, dependencySets[1].Dependencies.Count);

            Assert.Equal(new FrameworkName(".NETFramework, Version=4.0"), dependencySets[2].TargetFramework);
            Assert.Equal(2, dependencySets[2].Dependencies.Count);
            Assert.Equal("B", dependencySets[2].Dependencies.First().Id);
            Assert.Equal("E", dependencySets[2].Dependencies.Last().Id);

            Assert.Equal(new FrameworkName("Silverlight, Version=3.5"), dependencySets[3].TargetFramework);
            Assert.Equal(2, dependencySets[3].Dependencies.Count);
            Assert.Equal("D", dependencySets[3].Dependencies.First().Id);
            Assert.Equal("F", dependencySets[3].Dependencies.Last().Id);
        }

        [Fact]
        public void ManifestGroupReferenceSetsByTargetFrameworkAndPutNullFrameworkFirst()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    ReferenceSets = new List<ManifestReferenceSet> {
                            new ManifestReferenceSet {
                                TargetFramework = ".NETFramework40",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "B" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = null,
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "A" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = null,
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "C" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "Silverlight35",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "D" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "net40",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "E" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "sl35",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "F" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "winrt45",
                                References = new List<ManifestReference>() 
                            },
                    }
                }
            };

            // Act
            var referenceSets = ((IPackageMetadata)manifest.Metadata).PackageAssemblyReferences.ToList();

            // Assert
            Assert.Equal(4, referenceSets.Count);

            Assert.Null(referenceSets[0].TargetFramework);
            Assert.Equal(2, referenceSets[0].References.Count);
            Assert.Equal("A", referenceSets[0].References.First());
            Assert.Equal("C", referenceSets[0].References.Last());

            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), referenceSets[1].TargetFramework);
            Assert.Equal(0, referenceSets[1].References.Count);

            Assert.Equal(new FrameworkName(".NETFramework, Version=4.0"), referenceSets[2].TargetFramework);
            Assert.Equal(2, referenceSets[2].References.Count);
            Assert.Equal("B", referenceSets[2].References.First());
            Assert.Equal("E", referenceSets[2].References.Last());

            Assert.Equal(new FrameworkName("Silverlight, Version=3.5"), referenceSets[3].TargetFramework);
            Assert.Equal(2, referenceSets[3].References.Count);
            Assert.Equal("D", referenceSets[3].References.First());
            Assert.Equal("F", referenceSets[3].References.Last());
        }

        [Fact]
        public void ManifestGroupDependencySetsByTargetFrameworkAndPropertiesAndPutNullFrameworkFirst()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    DependencySets = new List<ManifestDependencySet> {
                            new ManifestDependencySet {
                                TargetFramework = ".NETFramework40",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "B" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "configuration", Value = "debug" }}
                            },

                            new ManifestDependencySet {
                                TargetFramework = null,
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "A" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "architecture", Value = "x86" }}
                            },

                            new ManifestDependencySet {
                                TargetFramework = null,
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "C" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "architecture", Value = "x64" }}
                            },

                            new ManifestDependencySet {
                                TargetFramework = "Silverlight35",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "D" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = "net40",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "E" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "configuration", Value = "debug" }}
                            },

                            new ManifestDependencySet {
                                TargetFramework = "sl35",
                                Dependencies = new List<ManifestDependency> 
                                    {
                                        new ManifestDependency { Id = "F" }
                                    }
                            },

                            new ManifestDependencySet {
                                TargetFramework = "winrt45",
                                Dependencies = new List<ManifestDependency>() 
                            },
                    }
                }
            };

            // Act
            var dependencySets = ((IPackageMetadata)manifest.Metadata).DependencySets.ToList();

            // Assert
            Assert.Equal(5, dependencySets.Count);

            Assert.Null(dependencySets[0].TargetFramework);
            Assert.Equal(1, dependencySets[0].Dependencies.Count);
            Assert.Equal("A", dependencySets[0].Dependencies.First().Id);
            Assert.Equal("x86", dependencySets[0].Properties.Where(p => p.Name == "architecture").Select(p => p.Value).Single());

            Assert.Null(dependencySets[1].TargetFramework);
            Assert.Equal(1, dependencySets[1].Dependencies.Count);
            Assert.Equal("C", dependencySets[1].Dependencies.Last().Id);
            Assert.Equal("x64", dependencySets[1].Properties.Where(p => p.Name == "architecture").Select(p => p.Value).Single());

            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), dependencySets[2].TargetFramework);
            Assert.Equal(0, dependencySets[2].Dependencies.Count);

            Assert.Equal(new FrameworkName(".NETFramework, Version=4.0"), dependencySets[3].TargetFramework);
            Assert.Equal(2, dependencySets[3].Dependencies.Count);
            Assert.Equal("B", dependencySets[3].Dependencies.First().Id);
            Assert.Equal("E", dependencySets[3].Dependencies.Last().Id);
            Assert.Equal("debug", dependencySets[3].Properties.Where(p => p.Name == "configuration").Select(p => p.Value).Single());

            Assert.Equal(new FrameworkName("Silverlight, Version=3.5"), dependencySets[4].TargetFramework);
            Assert.Equal(2, dependencySets[4].Dependencies.Count);
            Assert.Equal("D", dependencySets[4].Dependencies.First().Id);
            Assert.Equal("F", dependencySets[4].Dependencies.Last().Id);
        }

        [Fact]
        public void ManifestGroupReferenceSetsByTargetFrameworkAndPropertiesAndPutNullFrameworkFirst()
        {
            // Arrange
            var manifest = new Manifest
            {
                Metadata = new ManifestMetadata
                {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    ReferenceSets = new List<ManifestReferenceSet> {
                            new ManifestReferenceSet {
                                TargetFramework = ".NETFramework40",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "B" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "configuration", Value = "debug" }}
                            },

                            new ManifestReferenceSet {
                                TargetFramework = null,
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "A" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "architecture", Value = "x86" }}
                            },

                            new ManifestReferenceSet {
                                TargetFramework = null,
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "C" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "architecture", Value = "x64" }}
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "Silverlight35",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "D" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "net40",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "E" }
                                    },
                                Properties = new List<ManifestProperty>() { new ManifestProperty() { Name = "configuration", Value = "debug" }}
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "sl35",
                                References = new List<ManifestReference> 
                                    {
                                        new ManifestReference { File = "F" }
                                    }
                            },

                            new ManifestReferenceSet {
                                TargetFramework = "winrt45",
                                References = new List<ManifestReference>() 
                            },
                    }
                }
            };

            // Act
            var ReferenceSets = ((IPackageMetadata)manifest.Metadata).PackageAssemblyReferences.ToList();

            // Assert
            Assert.Equal(5, ReferenceSets.Count);

            Assert.Null(ReferenceSets[0].TargetFramework);
            Assert.Equal(1, ReferenceSets[0].References.Count);
            Assert.Equal("A", ReferenceSets[0].References.First());
            Assert.Equal("x86", ReferenceSets[0].Properties.Where(p => p.Name == "architecture").Select(p => p.Value).Single());

            Assert.Null(ReferenceSets[1].TargetFramework);
            Assert.Equal(1, ReferenceSets[1].References.Count);
            Assert.Equal("C", ReferenceSets[1].References.Last());
            Assert.Equal("x64", ReferenceSets[1].Properties.Where(p => p.Name == "architecture").Select(p => p.Value).Single());

            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), ReferenceSets[2].TargetFramework);
            Assert.Equal(0, ReferenceSets[2].References.Count);

            Assert.Equal(new FrameworkName(".NETFramework, Version=4.0"), ReferenceSets[3].TargetFramework);
            Assert.Equal(2, ReferenceSets[3].References.Count);
            Assert.Equal("B", ReferenceSets[3].References.First());
            Assert.Equal("E", ReferenceSets[3].References.Last());
            Assert.Equal("debug", ReferenceSets[3].Properties.Where(p => p.Name == "configuration").Select(p => p.Value).Single());

            Assert.Equal(new FrameworkName("Silverlight, Version=3.5"), ReferenceSets[4].TargetFramework);
            Assert.Equal(2, ReferenceSets[4].References.Count);
            Assert.Equal("D", ReferenceSets[4].References.First());
            Assert.Equal("F", ReferenceSets[4].References.Last());
        }

        // Test that manifest is serialized correctly.
        [Fact]
        public void ManifestSerialization()
        {
            var manifest = new Manifest();
            manifest.Metadata.Id = "id";
            manifest.Metadata.Authors = "author";
            manifest.Metadata.Version = "1.0.0";
            manifest.Metadata.Description = "description";
            
            manifest.Files = new List<ManifestFile>();            
            var file = new ManifestFile();
            file.Source = "file_source";
            file.Target = "file_target";
            manifest.Files.Add(file);

            var memoryStream = new MemoryStream();
            manifest.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // read the serialized manifest.
            var newManifest = Manifest.ReadFrom(memoryStream, validateSchema: true);
            Assert.Equal(newManifest.Metadata.Id, manifest.Metadata.Id);
            Assert.Equal(newManifest.Metadata.Authors, manifest.Metadata.Authors);
            Assert.Equal(newManifest.Metadata.Description, manifest.Metadata.Description);
            Assert.Equal(newManifest.Files.Count, manifest.Files.Count);
            for (int i = 0; i < newManifest.Files.Count; ++i)
            {
                Assert.Equal(newManifest.Files[i].Source, manifest.Files[i].Source);
                Assert.Equal(newManifest.Files[i].Target, manifest.Files[i].Target);
            }
        }

        [Theory]
        [InlineData("no group", 1, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <dependency id=""B"" />
                    <dependency id=""C"" />
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("one group", 1, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group targetFramework=""net40"">
                        <dependency id=""B"" />
                        <dependency id=""C"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("two groups (1,1)", 2, 2,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group targetFramework=""net40"">
                        <dependency id=""B"" />
                    </group>
                    <group targetFramework=""net45"">
                        <dependency id=""C"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("two groups (2,0)", 2, 2,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group targetFramework=""net40"">
                        <dependency id=""B"" />
                        <dependency id=""C"" />
                    </group>
                    <group targetFramework=""net45"">
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("one group with a property", 1, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <dependency id=""B"" />
                        <dependency id=""C"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("two groups with properties", 2, 2,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <dependency id=""B"" />
                    </group>
                    <group>
                        <property name=""architecture"" value=""x86"" />
                        <dependency id=""C"" />
                        <property name=""configuration"" value=""release"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("duplicate target framework", 2, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group targetFramework=""net40"">
                        <dependency id=""B"" />
                    </group>
                    <group targetFramework=""net40"">
                        <dependency id=""C"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("duplicate properties", 2, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <dependency id=""B"" />
                    </group>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <dependency id=""C"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        public void ValidateSchemaSucceedsForValidDependencies(string test, int rawGroupCount, int groupedSetCount, string content)
        {
            // Arrange
            // Switch to invariant culture to ensure the error message is in english.
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            // Act
            var metadata = Manifest.ReadFrom(content.AsStream(), validateSchema: true).Metadata;
            var groupCount = metadata.DependencySets.Count;
            var dependencySets = ((IPackageMetadata)metadata).DependencySets;
            var dependencies = dependencySets.SelectMany(s => s.Dependencies);

            // Assert
            Assert.Equal(rawGroupCount, groupCount);
            Assert.Equal(groupedSetCount, dependencySets.Count());

            Assert.Equal(2, dependencies.Count());
            Assert.Equal("B", dependencies.First().Id);
            Assert.Equal("C", dependencies.Last().Id);
        }

        [Theory]
        [InlineData("no groups",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <dependency />
                    <dependency version=""1.0.0"" />
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("mixed with targetFramework",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <dependency id=""B"" />
                    <group targetFramework=""net40"">
                        <dependency id=""C"" />
                    </group>
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("mixed with property",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <dependency id=""B"" />
                    </group>
                    <dependency id=""C"" />
                </dependencies>
              </metadata>
            </package>")]
        [InlineData("mixed with empty group with property",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <dependencies>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                    </group>
                    <dependency id=""B"" />
                    <dependency id=""C"" />
                </dependencies>
              </metadata>
            </package>")]
        public void ValidateSchemaFailsForInvalidDependencies(string test, string content)
        {
            // Arrange
            // Switch to invariant culture to ensure the error message is in english.
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            // Act & Assert - Some will be InvalidDataExceptions; some will be InvalidOperationException
            ExceptionAssert.Throws<Exception>(
                () => Manifest.ReadFrom(content.AsStream(), validateSchema: true));
        }


        [Theory]
        [InlineData("no group", 1, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <reference file=""B"" />
                    <reference file=""C"" />
                </references>
              </metadata>
            </package>")]
        [InlineData("one group", 1, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group targetFramework=""net40"">
                        <reference file=""B"" />
                        <reference file=""C"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("two groups (1,1)", 2, 2,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group targetFramework=""net40"">
                        <reference file=""B"" />
                    </group>
                    <group targetFramework=""net45"">
                        <reference file=""C"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("two groups (2,0)", 2, 2,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group targetFramework=""net40"">
                        <reference file=""B"" />
                        <reference file=""C"" />
                    </group>
                    <group targetFramework=""net45"">
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("one group with property", 1, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <reference file=""B"" />
                        <reference file=""C"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("two groups with properties", 2, 2,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <reference file=""B"" />
                    </group>
                    <group>
                        <property name=""architecture"" value=""x86"" />
                        <reference file=""C"" />
                        <property name=""configuration"" value=""release"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("duplicate target framework", 2, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group targetFramework=""net40"">
                        <reference file=""B"" />
                    </group>
                    <group targetFramework=""net40"">
                        <reference file=""C"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("duplicate properties", 2, 1,
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <reference file=""B"" />
                    </group>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <reference file=""C"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        public void ValidateSchemaSucceedsForValidReferences(string test, int rawGroupCount, int groupedSetCount, string content)
        {
            // Arrange
            // Switch to invariant culture to ensure the error message is in english.
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            // Act
            var metadata = Manifest.ReadFrom(content.AsStream(), validateSchema: true).Metadata;
            var groupCount = metadata.ReferenceSets.Count;
            var referenceSets = ((IPackageMetadata)metadata).PackageAssemblyReferences;
            var references = referenceSets.SelectMany(s => s.References);

            // Assert
            Assert.Equal(rawGroupCount, groupCount);
            Assert.Equal(groupedSetCount, referenceSets.Count);

            Assert.Equal(2, references.Count());
            Assert.Equal("B", references.First());
            Assert.Equal("C", references.Last());
        }

        [Theory]
        [InlineData("missing file",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <reference />
                </references>
              </metadata>
            </package>")]
        [InlineData("mixed",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <reference file=""B"" />
                    <group targetFramework=""net40"">
                        <reference file=""C"" />
                    </group>
                </references>
              </metadata>
            </package>")]
        [InlineData("mixed with property",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                        <reference file=""B"" />
                    </group>
                    <reference file=""C"" />
                </references>
              </metadata>
            </package>")]
        [InlineData("mixed with empty group with property",
            @"<?xml version=""1.0""?>
            <package xmlns=""http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd"">
              <metadata>
                <id>A</id>
                <version>1.0</version>
                <authors>jeffhandley</authors>
                <owners>jeffhandley</owners>
                <requireLicenseAcceptance>false</requireLicenseAcceptance>
                <description>Descriptions</description>
                <references>
                    <group>
                        <property name=""configuration"" value=""debug"" />
                    </group>
                    <reference file=""B"" />
                    <reference file=""C"" />
                </references>
              </metadata>
            </package>")]
        public void ValidateSchemaFailsForInvalidReferences(string test, string content)
        {
            // Arrange
            // Switch to invariant culture to ensure the error message is in english.
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            // Act & Assert - Some will be InvalidDataExceptions; some will be InvalidOperationException
            ExceptionAssert.Throws<Exception>(
                () => Manifest.ReadFrom(content.AsStream(), validateSchema: true));
        }

        private void AssertManifest(Manifest expected, Manifest actual)
        {
            Assert.Equal(expected.Metadata.Id, actual.Metadata.Id);
            Assert.Equal(expected.Metadata.Version, actual.Metadata.Version);
            Assert.Equal(expected.Metadata.Description, actual.Metadata.Description);
            Assert.Equal(expected.Metadata.Authors, actual.Metadata.Authors);
            Assert.Equal(expected.Metadata.Copyright, actual.Metadata.Copyright);
            Assert.Equal(expected.Metadata.IconUrl, actual.Metadata.IconUrl);
            Assert.Equal(expected.Metadata.Language, actual.Metadata.Language);
            Assert.Equal(expected.Metadata.LicenseUrl, actual.Metadata.LicenseUrl);
            Assert.Equal(expected.Metadata.LicenseNames, actual.Metadata.LicenseNames);
            Assert.Equal(expected.Metadata.Owners, actual.Metadata.Owners);
            Assert.Equal(expected.Metadata.ProjectUrl, actual.Metadata.ProjectUrl);
            Assert.Equal(expected.Metadata.RepositoryUrl, actual.Metadata.RepositoryUrl);
            Assert.Equal(expected.Metadata.RepositoryType, actual.Metadata.RepositoryType);
            Assert.Equal(expected.Metadata.ReleaseNotes, actual.Metadata.ReleaseNotes);
            Assert.Equal(expected.Metadata.RequireLicenseAcceptance, actual.Metadata.RequireLicenseAcceptance);
            Assert.Equal(expected.Metadata.DevelopmentDependency, actual.Metadata.DevelopmentDependency);
            Assert.Equal(expected.Metadata.Summary, actual.Metadata.Summary);
            Assert.Equal(expected.Metadata.Tags, actual.Metadata.Tags);
            Assert.Equal(expected.Metadata.MinClientVersion, actual.Metadata.MinClientVersion);

            if (expected.Metadata.Properties != null)
            {
                for (int i = 0; i < expected.Metadata.Properties.Count; i++)
                {
                    AssertProperty(expected.Metadata.Properties[i], actual.Metadata.Properties[i]);
                }
            }
            if (expected.Metadata.DependencySets != null)
            {
                for (int i = 0; i < expected.Metadata.DependencySets.Count; i++)
                {
                    AssertDependencySet(expected.Metadata.DependencySets[i], actual.Metadata.DependencySets[i]);
                }
            }
            if (expected.Metadata.FrameworkAssemblies != null)
            {
                for (int i = 0; i < expected.Metadata.FrameworkAssemblies.Count; i++)
                {
                    AssertFrameworkAssemblies(expected.Metadata.FrameworkAssemblies[i], actual.Metadata.FrameworkAssemblies[i]);
                }
            }
            if (expected.Metadata.ReferenceSets != null)
            {
                for (int i = 0; i < expected.Metadata.ReferenceSets.Count; i++)
                {
                    AssertReference(expected.Metadata.ReferenceSets[i], actual.Metadata.ReferenceSets[i]);
                }
            }
            if (expected.Files != null)
            {
                for (int i = 0; i < expected.Files.Count; i++)
                {
                    AssertFile(expected.Files[i], actual.Files[i]);
                }
            }
        }

        private void AssertFile(ManifestFile expected, ManifestFile actual)
        {
            Assert.Equal(expected.Source, actual.Source);
            Assert.Equal(expected.Target, actual.Target);
            Assert.Equal(expected.Exclude, actual.Exclude);
        }

        private static void AssertProperty(ManifestProperty expected, ManifestProperty actual)
        {
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Value, actual.Value);
        }

        private static void AssertDependencySet(ManifestDependencySet expected, ManifestDependencySet actual)
        {
            Assert.Equal(expected.TargetFramework, actual.TargetFramework);
            Assert.Equal(expected.Dependencies.Count, actual.Dependencies.Count);
            for (int i = 0; i < expected.Dependencies.Count; i++)
            {
                AssertDependency(expected.Dependencies[i], actual.Dependencies[i]);
            }
        }

        private static void AssertDependency(ManifestDependency expected, ManifestDependency actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Version, actual.Version);
        }

        private static void AssertFrameworkAssemblies(ManifestFrameworkAssembly expected, ManifestFrameworkAssembly actual)
        {
            Assert.Equal(expected.AssemblyName, actual.AssemblyName);
            Assert.Equal(expected.TargetFramework, actual.TargetFramework);
        }

        private static void AssertReference(ManifestReference expected, ManifestReference actual)
        {
            Assert.Equal(expected.File, actual.File);
        }

        private static void AssertReference(ManifestReferenceSet expected, ManifestReferenceSet actual)
        {
            Assert.Equal(expected.TargetFramework, actual.TargetFramework);
            Assert.Equal(expected.References.Count, actual.References.Count);
            for (int i = 0; i < expected.References.Count; i++)
            {
                AssertReference(expected.References[i], actual.References[i]);
            }
        }

        public static Stream CreateManifest(string id = "Test-Pack",
                                            string version = "1.0.0",
                                            string title = null,
                                            string authors = "NuGet Test",
                                            string owners = null,
                                            string licenseUrl = null,
                                            string licenseNames = null,
                                            string projectUrl = null,
                                            string repositoryUrl = null,
                                            string repositoryType = null,
                                            string iconUrl = null,
                                            bool? requiresLicenseAcceptance = null,
                                            bool? developmentDependency = null,
                                            string description = "Test description",
                                            string summary = null,
                                            string releaseNotes = null,
                                            string copyright = null,
                                            string language = null,
                                            string tags = null,
                                            IEnumerable<ManifestProperty> properties = null,
                                            IEnumerable<ManifestDependency> dependencies = null,
                                            IEnumerable<ManifestFrameworkAssembly> assemblyReference = null,
                                            IEnumerable<ManifestReferenceSet> references = null,
                                            IEnumerable<ManifestFile> files = null,
                                            string minClientVersion = null)
        {
            var document = new XDocument(new XElement("package"));
            var metadata = new XElement("metadata", new XElement("id", id), new XElement("version", version),
                                                    new XElement("description", description), new XElement("authors", authors));

            if (minClientVersion != null)
            {
                metadata.Add(new XAttribute("minClientVersion", minClientVersion));
            }
            
            document.Root.Add(metadata);

            if (title != null)
            {
                metadata.Add(new XElement("title", title));
            }
            if (owners != null)
            {
                metadata.Add(new XElement("owners", owners));
            }
            if (licenseUrl != null)
            {
                metadata.Add(new XElement("licenseUrl", licenseUrl));
            }
            if (licenseNames != null)
            {
                metadata.Add(new XElement("licenseNames", licenseNames));
            }
            if (projectUrl != null)
            {
                metadata.Add(new XElement("projectUrl", projectUrl));
            }
            if (repositoryUrl != null)
            {
                metadata.Add(new XElement("repositoryUrl", repositoryUrl));
            }
            if (repositoryType != null)
            {
                metadata.Add(new XElement("repositoryType", repositoryType));
            }
            if (iconUrl != null)
            {
                metadata.Add(new XElement("iconUrl", iconUrl));
            }
            if (requiresLicenseAcceptance != null)
            {
                metadata.Add(new XElement("requireLicenseAcceptance", requiresLicenseAcceptance.ToString().ToLowerInvariant()));
            }
            if (developmentDependency != null)
            {
                metadata.Add(new XElement("developmentDependency", developmentDependency.ToString().ToLowerInvariant()));
            }
            if (summary != null)
            {
                metadata.Add(new XElement("summary", summary));
            }
            if (releaseNotes != null)
            {
                metadata.Add(new XElement("releaseNotes", releaseNotes));
            }
            if (copyright != null)
            {
                metadata.Add(new XElement("copyright", copyright));
            }
            if (language != null)
            {
                metadata.Add(new XElement("language", language));
            }
            if (tags != null)
            {
                metadata.Add(new XElement("tags", tags));
            }
            if (properties != null)
            {
                metadata.Add(new XElement("properties",
                    properties.Select(p => new XElement("property", new XAttribute("name", p.Name), new XAttribute("value", p.Value)))));
            }
            if (dependencies != null)
            {
                metadata.Add(new XElement("dependencies",
                    dependencies.Select(d => new XElement("dependency", new XAttribute("id", d.Id), new XAttribute("version", d.Version)))));
            }
            if (assemblyReference != null)
            {
                metadata.Add(new XElement("frameworkAssemblies",
                    assemblyReference.Select(r => new XElement("frameworkAssembly",
                        new XAttribute("assemblyName", r.AssemblyName), new XAttribute("targetFramework", r.TargetFramework)))));
            }

            if (references != null)
            {
                if (references.Any(r => r.TargetFramework != null))
                {
                    metadata.Add(new XElement("references",
                        references.Select(r => new XElement("group",
                            r.TargetFramework != null ? new XAttribute("targetFramework", r.TargetFramework) : null,
                            r.References.Select(f => new XElement("reference", new XAttribute("file", f.File))))
                        )));
                }
                else
                {
                    metadata.Add(new XElement("references", references.SelectMany(r => r.References).Select(r => new XElement("reference", new XAttribute("file", r.File)))));
                }
            }

            if (files != null)
            {
                var filesNode = new XElement("files");
                foreach (var file in files)
                {
                    var fileNode = new XElement("file", new XAttribute("src", file.Source));
                    if (file.Target != null)
                    {
                        fileNode.Add(new XAttribute("target", file.Target));
                    }
                    if (file.Exclude != null)
                    {
                        fileNode.Add(new XAttribute("exclude", file.Exclude));
                    }

                    filesNode.Add(fileNode);
                }
                document.Root.Add(filesNode);
            }

            var stream = new MemoryStream();
            document.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
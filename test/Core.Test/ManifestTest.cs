using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using System.Runtime.Versioning;

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
                    References = new List<ManifestReference> {
                        new ManifestReference { File = "Foo?.dll" },
                        new ManifestReference { File = "Bar*.dll" },
                        new ManifestReference { File = @"net40\baz.dll" }
                    }
                },
                Files = new List<ManifestFile> {
                    new ManifestFile { Source = "Foo.dll", Target = "lib" }
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest),
                "Assembly reference 'Foo?.dll' contains invalid characters.\r\nAssembly reference 'Bar*.dll' contains invalid characters.\r\nAssembly reference 'net40\\baz.dll' contains invalid characters.");
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
            var manifest = Manifest.ReadFrom(manifestStream);

            // Assert
            AssertManifest(expectedManifest, manifest);
        }

        [Fact]
        public void ReadFromReadsAllMetadataValues()
        {
            // Arrange
            var manifestStream = CreateManifest(id: "Test-Pack2", version: "1.0.0-alpha", title: "blah", authors: "Outercurve",
                licenseUrl: "http://nuget.org/license", projectUrl: "http://nuget.org/project", iconUrl: "https://nuget.org/icon",
                requiresLicenseAcceptance: true, description: "This is not a description", summary: "This is a summary", releaseNotes: "Release notes",
                copyright: "Copyright 2012", language: "fr-FR", tags: "Test Unit",
                dependencies: new[] { new ManifestDependency { Id = "Test", Version = "1.2.0" } },
                assemblyReference: new[] { new ManifestFrameworkAssembly { AssemblyName = "System.Data", TargetFramework = "4.0" } },
                references: new[] { "Test.dll" }
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
                    ProjectUrl = "http://nuget.org/project",
                    IconUrl = "https://nuget.org/icon",
                    RequireLicenseAcceptance = true,
                    Summary = "This is a summary",
                    ReleaseNotes = "Release notes",
                    Copyright = "Copyright 2012",
                    Language = "fr-FR",
                    Tags = "Test Unit",
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
                    References = new List<ManifestReference> { new ManifestReference { File = "Test.dll" } }
                }
            };

            // Act 
            var manifest = Manifest.ReadFrom(manifestStream);

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
            var manifest = Manifest.ReadFrom(manifestStream);

            // Assert
            AssertManifest(expectedManifest, manifest);
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

            Assert.Equal(new FrameworkName(".NETFramework, Version=4.0"), dependencySets[1].TargetFramework);
            Assert.Equal(2, dependencySets[1].Dependencies.Count);
            Assert.Equal("B", dependencySets[1].Dependencies.First().Id);
            Assert.Equal("E", dependencySets[1].Dependencies.Last().Id);

            Assert.Equal(new FrameworkName("Silverlight, Version=3.5"), dependencySets[2].TargetFramework);
            Assert.Equal(2, dependencySets[2].Dependencies.Count);
            Assert.Equal("D", dependencySets[2].Dependencies.First().Id);
            Assert.Equal("F", dependencySets[2].Dependencies.Last().Id);

            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), dependencySets[3].TargetFramework);
            Assert.Equal(0, dependencySets[3].Dependencies.Count);
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
            Assert.Equal(expected.Metadata.Owners, actual.Metadata.Owners);
            Assert.Equal(expected.Metadata.ProjectUrl, actual.Metadata.ProjectUrl);
            Assert.Equal(expected.Metadata.ReleaseNotes, actual.Metadata.ReleaseNotes);
            Assert.Equal(expected.Metadata.RequireLicenseAcceptance, actual.Metadata.RequireLicenseAcceptance);
            Assert.Equal(expected.Metadata.Summary, actual.Metadata.Summary);
            Assert.Equal(expected.Metadata.Tags, actual.Metadata.Tags);

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
            if (expected.Metadata.References != null)
            {
                for (int i = 0; i < expected.Metadata.References.Count; i++)
                {
                    AssertReference(expected.Metadata.References[i], actual.Metadata.References[i]);
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

        public static Stream CreateManifest(string id = "Test-Pack",
                                            string version = "1.0.0",
                                            string title = null,
                                            string authors = "NuGet Test",
                                            string owners = null,
                                            string licenseUrl = null,
                                            string projectUrl = null,
                                            string iconUrl = null,
                                            bool? requiresLicenseAcceptance = null,
                                            string description = "Test description",
                                            string summary = null,
                                            string releaseNotes = null,
                                            string copyright = null,
                                            string language = null,
                                            string tags = null,
                                            IEnumerable<ManifestDependency> dependencies = null,
                                            IEnumerable<ManifestFrameworkAssembly> assemblyReference = null,
                                            IEnumerable<string> references = null,
                                            IEnumerable<ManifestFile> files = null)
        {
            var document = new XDocument(new XElement("package"));
            var metadata = new XElement("metadata", new XElement("id", id), new XElement("version", version),
                                                    new XElement("description", description), new XElement("authors", authors));
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
            if (projectUrl != null)
            {
                metadata.Add(new XElement("projectUrl", projectUrl));
            }
            if (iconUrl != null)
            {
                metadata.Add(new XElement("iconUrl", iconUrl));
            }
            if (requiresLicenseAcceptance != null)
            {
                metadata.Add(new XElement("requireLicenseAcceptance", requiresLicenseAcceptance.ToString().ToLowerInvariant()));
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
                metadata.Add(new XElement("references", references.Select(r => new XElement("reference", new XAttribute("file", r)))));
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

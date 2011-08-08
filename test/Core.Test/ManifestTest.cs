using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class ManifestTest {
        [TestMethod]
        public void ManifestValidatesMetadata() {
            // Arrange
            var manifest = new Manifest {
                Metadata = new ManifestMetadata {
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

        [TestMethod]
        public void ManifestValidatesMetadataUrlsIfEmpty() {
            // Arrange
            var manifest = new Manifest {
                Metadata = new ManifestMetadata {
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

        [TestMethod]
        public void ManifestValidatesManifestFiles() {
            // Arrange
            var manifest = new Manifest {
                Metadata = new ManifestMetadata {
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

        [TestMethod]
        public void ManifestEnsuresManifestReferencesDoNotContainInvalidCharacters() {
            // Arrange
            var manifest = new Manifest {
                Metadata = new ManifestMetadata {
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

        [TestMethod]
        public void ManifestValidatesDependencies() {
            // Arrange
            var manifest = new Manifest {
                Metadata = new ManifestMetadata {
                    Id = "Foobar",
                    Version = "1.0",
                    Authors = "test-author",
                    Description = "desc",
                    Dependencies = new List<ManifestDependency> {
                        new ManifestDependency { Id = null }
                    }
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest), "Dependency Id is required.");
        }
    }
}

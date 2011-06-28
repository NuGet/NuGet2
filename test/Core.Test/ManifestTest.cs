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
                    }
                }
            };

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => Manifest.Validate(manifest),
                "Source path '|' contains invalid characters.\r\nTarget path '<' contains invalid characters.\r\nSource path 'foo\\bar\\|>' contains invalid characters.");
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

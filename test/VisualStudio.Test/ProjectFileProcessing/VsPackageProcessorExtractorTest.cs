using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NuGet.VisualStudio.Test.ProjectFileProcessing
{
    public class VsPackageProcessorExtractorTest
    {
        [Fact]
        public void FromManifestFilesCanBeCalledWithNull()
        {
            // Arrange
            var extractor = new VsPackageProcessorExtractor();

            // Act
            var result = extractor.FromManifestFiles(null);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void FromManifestFilesFindsCustomTool()
        {
            // Arrange
            const string toolName = "Ammer";
            var file = new ManifestFile
                {
                    Source = "*.settings",
                    Properties = new List<ManifestFileProperty>
                        {
                            new ManifestFileProperty
                                {
                                    Name = "CustomTool",
                                    Value = toolName
                                }
                        }
                };

            var extractor = new VsPackageProcessorExtractor();

            // Act
            var result = extractor.FromManifestFiles(
                new[] { new PackageManifestFile(file) })
                            .OfType<VsProjectItemCustomToolSetter>()
                            .Single();

            // Assert
            Assert.Equal(toolName, result.CustomToolName);
        }

        [Fact]
        public void FromManifestFilesFindsCustomToolAndNamespace()
        {
            // Arrange
            const string toolNamespace = "NuGet.ProjectFileProcessing";
            var file = new ManifestFile
                {
                    Source = "*.settings",
                    Properties = new List<ManifestFileProperty>
                        {
                            new ManifestFileProperty
                                {
                                    Name = "CustomTool",
                                    Value = "Ammer"
                                },
                            new ManifestFileProperty
                                {
                                    Name = "CustomToolNamespace",
                                    Value = toolNamespace
                                }
                        }
                };

            var extractor = new VsPackageProcessorExtractor();

            // Act
            var result = extractor.FromManifestFiles(
                new[] {new PackageManifestFile(file)})
                            .OfType<VsProjectItemCustomToolSetter>()
                            .Single();

            // Assert
            Assert.Equal(toolNamespace, result.CustomToolNamespace);
        }

        [Fact]
        public void FromManifestFilesFindsPropertySetters()
        {
            // Arrange
            const string pattern = "*.settings";
            const string propertyName = "PropertyName";
            const string propertyValue = "PropertyValue";
            var file = new ManifestFile
                {
                    Source = pattern,
                    Properties = new List<ManifestFileProperty>
                        {
                            new ManifestFileProperty
                                {
                                    Name = propertyName,
                                    Value = propertyValue
                                }
                        }
                };

            var extractor = new VsPackageProcessorExtractor();

            // Act
            var result = extractor.FromManifestFiles(
                new[] {new PackageManifestFile(file)})
                            .OfType<VsProjectItemPropertySetter>()
                            .Single();

            // Assert
            Assert.Equal(pattern, result.MatchPattern);
            Assert.Equal(propertyName, result.PropertyName);
            Assert.Equal(propertyValue, result.PropertyValue);
        }
    }
}
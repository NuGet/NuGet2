using System.Collections.Generic;
using System.Linq;
using NuGet.Authoring;
using Xunit;

namespace NuGet.VisualStudio.Test.ProjectFileProcessing
{
    public class VsPackageProcessorExtractorTest
    {
        [Fact]
        public void FromManifestFilesCanBeCalledWithNull()
        {
            var sut = new VsPackageProcessorExtractor();

            var result = sut.FromManifestFiles(null);

            Assert.NotNull(result);
        }

        [Fact]
        public void FromManifestFilesFindsCustomTool()
        {
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

            var sut = new VsPackageProcessorExtractor();

            var result = sut.FromManifestFiles(
                new[] {new PackageManifestFile(file)})
                            .OfType<VsProjectItemCustomToolSetter>()
                            .Single();

            Assert.Equal(toolName, result.CustomToolName);
        }

        [Fact]
        public void FromManifestFilesFindsCustomToolAndNamespace()
        {
            const string toolNamespace = "Nuget.ProjectFileProcessing";
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

            var sut = new VsPackageProcessorExtractor();

            var result = sut.FromManifestFiles(
                new[] {new PackageManifestFile(file)})
                            .OfType<VsProjectItemCustomToolSetter>()
                            .Single();

            Assert.Equal(toolNamespace, result.CustomToolNamespace);
        }

        [Fact]
        public void FromManifestFilesFindsPropertySetters()
        {
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

            var sut = new VsPackageProcessorExtractor();

            var result = sut.FromManifestFiles(
                new[] {new PackageManifestFile(file)})
                            .OfType<VsProjectItemPropertySetter>()
                            .Single();

            Assert.Equal(pattern, result.MatchPattern);
            Assert.Equal(propertyName, result.PropertyName);
            Assert.Equal(propertyValue, result.PropertyValue);
        }
    }
}
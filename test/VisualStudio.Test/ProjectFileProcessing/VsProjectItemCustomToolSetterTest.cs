using System;
using Moq;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectItemCustomToolSetterTest
    {
        const string MatchPattern = "*.txt";
        const string CustomTool = "CustomTool";
        const string CustomToolNamespace = "CustomToolNamespace";

        [Fact]
        public void MatchPatternCanNotBeNull()
        {
            var ex =
                Assert.Throws<ArgumentException>(
                    () => GetSut(null, CustomTool, CustomToolNamespace));

            Assert.Equal("matchPattern", ex.ParamName);
        }

        [Fact]
        public void CustomToolPropertyCanNotBeNull()
        {
            var ex =
                Assert.Throws<ArgumentException>(
                    () => GetSut(MatchPattern, null, CustomToolNamespace));

            Assert.Equal("customTool", ex.ParamName);
        }

        [Fact]
        public void CustomToolNamespacePropertyCanBeNull()
        {
            Assert.DoesNotThrow(
                () => GetSut(MatchPattern, CustomTool, null));
        }

        [Fact]
        public void SetsTheCustomToolProperty()
        {
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .Setup(o => o.SetPropertyValue(
                    VsProjectItemCustomToolSetter.CustomToolPropertyName,
                    CustomTool))
                .Verifiable();

            var sut = GetSut(
                MatchPattern, CustomTool, CustomToolNamespace);

            sut.Process(projectItemMock.Object);

            projectItemMock.Verify();
        }

        [Fact]
        public void SetsTheCustomToolNamespaceProperty()
        {
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .Setup(o => o.SetPropertyValue(
                    VsProjectItemCustomToolSetter.CustomToolNamespacePropertyName,
                    CustomToolNamespace))
                .Verifiable();

            var sut = GetSut(
                MatchPattern, CustomTool, CustomToolNamespace);

            sut.Process(projectItemMock.Object);

            projectItemMock.Verify();
        }

        [Fact]
        public void CallsRunCustomTool()
        {
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .Setup(o => o.RunCustomTool())
                .Verifiable();

            var sut = GetSut(
                MatchPattern, CustomTool, CustomToolNamespace);

            sut.Process(projectItemMock.Object);

            projectItemMock.Verify();
        }

        static VsProjectItemCustomToolSetter GetSut(
            string matchPattern,
            string customTool, string customToolNamespace)
        {
            return new VsProjectItemCustomToolSetter(
                matchPattern,
                customTool, customToolNamespace);
        }
    }
}
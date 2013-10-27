using System;
using Moq;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectItemCustomToolSetterTest
    {
        private const string MatchPattern = "*.txt";
        private const string CustomTool = "CustomTool";
        private const string CustomToolNamespace = "CustomToolNamespace";

        [Fact]
        public void MatchPatternCanNotBeNull()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(() => new VsProjectItemCustomToolSetter(null, CustomTool, CustomToolNamespace));

            // Assert
            Assert.Equal("matchPattern", ex.ParamName);
        }

        [Fact]
        public void CustomToolPropertyCanNotBeNull()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(() => new VsProjectItemCustomToolSetter(MatchPattern, null, CustomToolNamespace));

            // Assert
            Assert.Equal("customToolName", ex.ParamName);
        }

        [Fact]
        public void CustomToolNamespacePropertyCanBeNull()
        {
            // Arrange, Act, and Assert
            Assert.DoesNotThrow(() => new VsProjectItemCustomToolSetter(MatchPattern, CustomTool, null));
        }

        [Fact]
        public void SetsTheCustomToolProperty()
        {
            // Arrange
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .Setup(o => o.SetPropertyValue(
                    VsProjectItemCustomToolSetter.CustomToolPropertyName,
                    CustomTool))
                .Verifiable();

            var setter = new VsProjectItemCustomToolSetter(MatchPattern, CustomTool, CustomToolNamespace);

            // Act
            setter.Process(projectItemMock.Object);

            // Assert
            projectItemMock.Verify();
        }

        [Fact]
        public void SetsTheCustomToolNamespaceProperty()
        {
            // Arrange
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .Setup(o => o.SetPropertyValue(
                    VsProjectItemCustomToolSetter.CustomToolNamespacePropertyName,
                    CustomToolNamespace))
                .Verifiable();

            var setter = new VsProjectItemCustomToolSetter(MatchPattern, CustomTool, CustomToolNamespace);

            // Act
            setter.Process(projectItemMock.Object);

            // Assert
            projectItemMock.Verify();
        }

        [Fact]
        public void CallsRunCustomTool()
        {
            // Arrange
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .Setup(o => o.RunCustomTool())
                .Verifiable();

            var setter = new VsProjectItemCustomToolSetter(MatchPattern, CustomTool, CustomToolNamespace);

            // Act
            setter.Process(projectItemMock.Object);

            // Assert
            projectItemMock.Verify();
        }
    }
}
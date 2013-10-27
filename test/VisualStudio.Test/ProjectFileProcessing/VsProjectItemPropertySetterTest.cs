using Moq;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectItemPropertySetterTest
    {
        private const string TargetPath = "File.txt";
        private const string PropertyName = "PropertyName";
        private const string PropertyValue = "PropertyValue";

        private static Mock<IProjectFileProcessingProjectItem> GetProjectItemMock()
        {
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .SetupGet(m => m.Path)
                .Returns(TargetPath);
            projectItemMock
                .Setup(o => o.SetPropertyValue(PropertyName, PropertyValue));

            return projectItemMock;
        }

        private static Mock<IProjectFileProcessingProject> GetProjectMock(IProjectFileProcessingProjectItem projectItem)
        {
            var projectMock = new Mock<IProjectFileProcessingProject>();
            projectMock
                .Setup(o => o.GetItem(TargetPath))
                .Returns(projectItem);

            return projectMock;
        }

        [Fact]
        public void VsPropertySetterSetsPropertyOnMatch()
        {
            // Arrange
            var projectItemMock = GetProjectItemMock();
            var projectMock = GetProjectMock(projectItemMock.Object);
            var processor = new VsProjectItemPropertySetter("*.txt", PropertyName, PropertyValue);

            var builder = new ProjectFileProcessingBuilder(null)
                .WithProcessor(processor)
                .Build(projectMock.Object);

            // Act
            builder.Process(TargetPath);

            // Assert
            projectItemMock.Verify(
                o => o.SetPropertyValue(PropertyName, PropertyValue),
                Times.Once());
        }

        [Fact]
        public void VsPropertySetterDoesNotSetPropertyOnMisMatch()
        {
            // Arrange
            var projectItemMock = GetProjectItemMock();
            var projectMock = GetProjectMock(projectItemMock.Object);
            var processor = new VsProjectItemPropertySetter("*.xxx", PropertyName, PropertyValue);

            var builder = new ProjectFileProcessingBuilder(null)
                .WithProcessor(processor)
                .Build(projectMock.Object);

            // Act
            builder.Process(TargetPath);

            // Assert
            projectItemMock.Verify(
                o => o.SetPropertyValue(PropertyName, PropertyValue),
                Times.Never());
        }

        [Fact]
        public void VsPropertySetterDoesNothingIfProjectItemNotFound()
        {
            // Arrange
            var projectItemMock = GetProjectItemMock();
            var projectMock = GetProjectMock(projectItemMock.Object);
            var processor = new VsProjectItemPropertySetter("*.txt", PropertyName, PropertyValue);

            var builder = new ProjectFileProcessingBuilder(null)
                .WithProcessor(processor)
                .Build(projectMock.Object);

            // Act
            builder.Process("XXX");

            // Assert
            projectItemMock.Verify(
                o => o.SetPropertyValue(PropertyName, PropertyValue),
                Times.Never());
        }
    }
}
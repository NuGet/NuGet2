using System;
using Moq;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectItemPropertySetterTest
    {
        const string TargetPath = "File.txt";
        const string PropertyName = "PropertyName";
        const string PropertyValue = "PropertyValue";

        static Mock<IProjectFileProcessingProjectItem> GetProjectItemMock()
        {
            var projectItemMock = new Mock<IProjectFileProcessingProjectItem>();
            projectItemMock
                .SetupGet(m => m.Path)
                .Returns(TargetPath);
            projectItemMock
                .Setup(o => o.SetPropertyValue(PropertyName, PropertyValue));

            return projectItemMock;
        }

        static Mock<IProjectFileProcessingProject> GetProjectMock(
            IProjectFileProcessingProjectItem projectItem)
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
            var projectItemMock = GetProjectItemMock();
            var projectMock = GetProjectMock(projectItemMock.Object);

            var sut = new ProjectFileProcessingBuilder(null)
                .WithVsPropertySetter("*.txt", PropertyName, PropertyValue)
                .Build(projectMock.Object);

            // act
            sut.Process(TargetPath);

            // assert
            projectItemMock.Verify(
                o => o.SetPropertyValue(PropertyName, PropertyValue),
                Times.Once());
        }

        [Fact]
        public void VsPropertySetterDoesNotSetPropertyOnMisMatch()
        {
            var projectItemMock = GetProjectItemMock();
            var projectMock = GetProjectMock(projectItemMock.Object);

            var sut = new ProjectFileProcessingBuilder(null)
                .WithVsPropertySetter("*.xxx", PropertyName, PropertyValue)
                .Build(projectMock.Object);

            // act
            sut.Process(TargetPath);

            // assert
            projectItemMock.Verify(
                o => o.SetPropertyValue(PropertyName, PropertyValue),
                Times.Never());
        }
    }
}
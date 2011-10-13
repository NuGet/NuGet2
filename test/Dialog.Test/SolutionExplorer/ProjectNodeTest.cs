using System.Linq;
using Xunit;


namespace NuGet.Dialog.Test.SolutionExplorer
{

    public class ProjectNodeTest
    {
        [Fact]
        public void NamePropertyIsCorrect()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act & Assert
            Assert.Equal("A", node.Name);
        }

        [Fact]
        public void IsSelectedFalseByDefault()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act & Assert
            Assert.True(node.IsSelected == false);
        }

        [Fact]
        public void ProjectPropertyIsCorrect()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act & Assert
            Assert.Same(project, node.Project);
        }

        [Fact]
        public void GetSelectedProjectReturnsProjectIfIsSelectedIsTrueAndIsEnabledIsTrue()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act
            node.IsSelected = true;
            node.IsEnabled = true;

            // Assert
            Assert.Same(project, node.GetSelectedProjects().Single());
        }

        [Fact]
        public void GetSelectedProjectReturnsEmptyIfIsSelectedIsFalse()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act
            node.IsSelected = false;

            // Assert
            Assert.False(node.GetSelectedProjects().Any());
        }

        [Fact]
        public void GetSelectedProjectReturnsEmptyIfIsSelectedIsFalseOrIsEnabledIsFalse()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act
            node.IsSelected = false;
            node.IsEnabled = false;
            var result1 = node.GetSelectedProjects();

            node.IsSelected = true;
            node.IsEnabled = false;
            var result2 = node.GetSelectedProjects();

            node.IsSelected = false;
            node.IsEnabled = true;
            var result3 = node.GetSelectedProjects();

            // Assert
            Assert.False(result1.Any());
            Assert.False(result2.Any());
            Assert.False(result3.Any());
        }

        [Fact]
        public void ParentPropertyIsNullByDefault()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act && Assert
            Assert.Null(node.Parent);
        }

        [Fact]
        public void ChangingIsSelectedPropertyRaisePropertyChangedEvent()
        {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);
            bool called = false;
            node.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "IsSelected")
                {
                    called = true;
                }
            };

            // Act
            node.IsSelected = true;

            // Assert
            Assert.True(called);
        }

    }
}

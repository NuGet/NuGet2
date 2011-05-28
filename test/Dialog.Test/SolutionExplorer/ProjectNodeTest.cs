using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Dialog.Test.SolutionExplorer {
    [TestClass]
    public class ProjectNodeTest {
        [TestMethod]
        public void NamePropertyIsCorrect() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act & Assert
            Assert.AreEqual("A", node.Name);
        }

        [TestMethod]
        public void IsSelectedFalseByDefault() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act & Assert
            Assert.IsTrue(node.IsSelected == false);
        }

        [TestMethod]
        public void ProjectPropertyIsCorrect() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act & Assert
            Assert.AreSame(project, node.Project);
        }

        [TestMethod]
        public void GetSelectedProjectReturnsProjectIfIsSelectedIsTrueAndIsEnabledIsTrue() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act
            node.IsSelected = true;
            node.IsEnabled = true;

            // Assert
            Assert.AreSame(project, node.GetSelectedProjects().Single());
        }

        [TestMethod]
        public void GetSelectedProjectReturnsEmptyIfIsSelectedIsFalse() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act
            node.IsSelected = false;

            // Assert
            Assert.IsFalse(node.GetSelectedProjects().Any());
        }

        [TestMethod]
        public void GetSelectedProjectReturnsEmptyIfIsSelectedIsFalseOrIsEnabledIsFalse() {
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
            Assert.IsFalse(result1.Any());
            Assert.IsFalse(result2.Any());
            Assert.IsFalse(result3.Any());
        }

        [TestMethod]
        public void ParentPropertyIsNullByDefault() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);

            // Act && Assert
            Assert.IsNull(node.Parent);
        }

        [TestMethod]
        public void ChangingIsSelectedPropertyRaisePropertyChangedEvent() {
            // Arrange
            var project = MockProjectUtility.CreateMockProject("A");
            var node = new ProjectNode(project);
            bool called = false;
            node.PropertyChanged += (o, e) => {
                if (e.PropertyName == "IsSelected") {
                    called = true;
                }
            };

            // Act
            node.IsSelected = true;

            // Assert
            Assert.IsTrue(called);
        }

    }
}

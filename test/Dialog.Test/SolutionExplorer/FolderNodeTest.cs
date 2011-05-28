using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Dialog.Test.SolutionExplorer {

    [TestClass]
    public class FolderNodeTest {

        [TestMethod]
        public void PropertyNameIsCorrect() {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            Assert.AreEqual("A", node.Name);
        }

        [TestMethod]
        public void IsSelectedSetToFalseByDefault() {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            Assert.IsTrue(node.IsSelected == false);
        }

        [TestMethod]
        public void PropertyChildrenIsCorrect() {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            Assert.AreEqual(3, node.Children.Count);
        }

        [TestMethod]
        public void TestParentPropertyOfChildrenAreSetToParentFolder() {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            foreach (var child in node.Children) {
                Assert.AreSame(node, child.Parent);
            }
        }

        [TestMethod]
        public void SelectParentWillSelectAllChildren() {
            // Arrange
            var node = CreateFolderNode();

            // Act
            node.IsSelected = true;

            // Assert
            foreach (var child in node.Children) {
                Assert.IsTrue(child.IsSelected == true);
            }
        }

        [TestMethod]
        public void SelectParentWillSelectAllChildrenExceptTheDisabledOnes() {
            // Arrange
            ProjectNode[] children = new ProjectNode[3];

            // make two children enabled, one disabled
            for (int i = 0; i < 3; i++) {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project) {
                    IsEnabled = i % 2 == 0
                };
                children[i] = node;
            }

            var folder = new FolderNode("A", children);

            // Act
            folder.IsSelected = true;

            // Assert
            Assert.IsTrue(children[0].IsSelected == true);
            Assert.IsTrue(children[1].IsSelected == false);
            Assert.IsTrue(children[2].IsSelected == true);
        }

        [TestMethod]
        public void UnselectParentWillUnselectAllChildren() {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;
            foreach (var child in node.Children) {
                Assert.IsTrue(child.IsSelected == true);
            }

            // Act
            node.IsSelected = false;

            // Assert
            foreach (var child in node.Children) {
                Assert.IsTrue(child.IsSelected == false);
            }
        }

        [TestMethod]
        public void UnselectParentWillUnselectAllChildrenExceptTheDisabledOnes() {
            // Arrange
            ProjectNode[] children = new ProjectNode[3];

            // make two children enabled, one disabled
            for (int i = 0; i < 3; i++) {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project) {
                    IsSelected = true,
                    IsEnabled = i % 2 == 0,
                };
                children[i] = node;
            }

            var folder = new FolderNode("A", children);

            Assert.IsTrue(children[0].IsSelected == true);
            Assert.IsTrue(children[1].IsSelected == true);
            Assert.IsTrue(children[2].IsSelected == true);

            // Act
            folder.IsSelected = false;

            // Assert
            Assert.IsTrue(children[0].IsSelected == false);
            Assert.IsTrue(children[1].IsSelected == true);
            Assert.IsTrue(children[2].IsSelected == false);
        }

        [TestMethod]
        public void SelectAllChildrenWillAlsoSelectParent() {
            // Arrange
            var node = CreateFolderNode();

            // Act
            foreach (var child in node.Children) {
                child.IsSelected = true;
            }

            // Assert
            Assert.IsTrue(node.IsSelected == true);
        }

        [TestMethod]
        public void UnselectAllChildrenWillAlsoUnselectParent() {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;

            // Act
            foreach (var child in node.Children) {
                child.IsSelected = false;
            }

            // Assert
            Assert.IsTrue(node.IsSelected == false);
        }

        [TestMethod]
        public void SelectOneOfTheChildrenWillChangeParentToUnderminateState() {
            // Arrange
            var node = CreateFolderNode();

            // Act
            var firstNode = node.Children.First();
            firstNode.IsSelected = true;

            // Assert
            Assert.IsTrue(node.IsSelected == null);
        }

        [TestMethod]
        public void UnselectOneOfTheChildrenWillChangeParentToUnderminateState() {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;

            // Act
            var firstNode = node.Children.First();
            firstNode.IsSelected = false;

            // Assert
            Assert.IsTrue(node.IsSelected == null);
        }

        [TestMethod]
        public void ParentFolderDoNotPropagateStateBackToChildrenWhenTheyAreFirstAddedToFolder() {
            // Arrange
            ProjectNode[] children = new ProjectNode[3];

            // make two children selected, one unselected
            for (int i = 0; i < 3; i++) {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project) {
                    IsSelected = i % 2 == 0
                };
                children[i] = node;
            }

            var folder = new FolderNode("A", children);

            // Act
            var root = new FolderNode("Root", new[] { folder });

            // Assert
            Assert.IsNull(folder.IsSelected);
            for (int i = 0; i < 3; i++) {
                Assert.IsTrue(children[0].IsSelected == true);
                Assert.IsTrue(children[1].IsSelected == false);
                Assert.IsTrue(children[2].IsSelected == true);
            }
        }

        [TestMethod]
        public void ParentNodeIsDisabledIfAllChildrenAreDisabled() {
            // Arrange
            var children = new ProjectNode[3];

            // disable all children
            for (int i = 0; i < 3; i++) {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project) {
                    IsEnabled = false
                };
                children[i] = node;
            }

            // Act
            var folder = new FolderNode("A", children);

            // Assert
            Assert.IsFalse(folder.IsEnabled);
        }

        [TestMethod]
        public void ParentNodeIsEnabledIfAtLeastOneChildrenIsEnabled() {
            // Arrange
            var children = new ProjectNode[3];

            // make two children enabled, one disabled
            for (int i = 0; i < 3; i++) {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project) {
                    IsEnabled = i % 2 == 0
                };
                children[i] = node;
            }

            // Act
            var folder = new FolderNode("A", children);

            // Assert
            Assert.IsTrue(folder.IsEnabled);
        }

        private FolderNode CreateFolderNode(string name = "A", ICollection<ProjectNodeBase> children = null) {
            if (children == null) {
                children = new List<ProjectNodeBase>();

                for (int i = 0; i < 3; i++) {
                    var project = MockProjectUtility.CreateMockProject("p" + i);
                    var node = new ProjectNode(project);
                    children.Add(node);
                }
            }

            return new FolderNode(name, children);
        }
    }
}
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
        public void SelectOnOfTheChildrenWillChangeParentToUnderminateState() {
            // Arrange
            var node = CreateFolderNode();

            // Act
            var firstNode = node.Children.First();
            firstNode.IsSelected = true;

            // Assert
            Assert.IsTrue(node.IsSelected == null);
        }

        [TestMethod]
        public void UnselectOnOfTheChildrenWillChangeParentToUnderminateState() {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;

            // Act
            var firstNode = node.Children.First();
            firstNode.IsSelected = false;

            // Assert
            Assert.IsTrue(node.IsSelected == null);
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
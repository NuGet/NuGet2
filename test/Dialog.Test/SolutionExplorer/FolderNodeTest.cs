using System.Collections.Generic;
using System.Linq;
using Xunit;


namespace NuGet.Dialog.Test.SolutionExplorer
{


    public class FolderNodeTest
    {

        [Fact]
        public void PropertyNameIsCorrect()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            Assert.Equal("A", node.Name);
        }

        [Fact]
        public void IsSelectedSetToFalseByDefault()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            Assert.True(node.IsSelected == false);
        }

        [Fact]
        public void PropertyChildrenIsCorrect()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            Assert.Equal(3, node.Children.Count);
        }

        [Fact]
        public void TestParentPropertyOfChildrenAreSetToParentFolder()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act && Assert
            foreach (var child in node.Children)
            {
                Assert.Same(node, child.Parent);
            }
        }

        [Fact]
        public void SelectParentWillSelectAllChildren()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act
            node.IsSelected = true;

            // Assert
            foreach (var child in node.Children)
            {
                Assert.True(child.IsSelected == true);
            }
        }

        [Fact]
        public void SelectParentWillSelectAllChildrenExceptTheDisabledOnes()
        {
            // Arrange
            ProjectNode[] children = new ProjectNode[3];

            // make two children enabled, one disabled
            for (int i = 0; i < 3; i++)
            {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project)
                {
                    IsEnabled = i % 2 == 0
                };
                children[i] = node;
            }

            var folder = new FolderNode(null, "A", children);

            // Act
            folder.IsSelected = true;

            // Assert
            Assert.True(children[0].IsSelected == true);
            Assert.True(children[1].IsSelected == false);
            Assert.True(children[2].IsSelected == true);
        }

        [Fact]
        public void UnselectParentWillUnselectAllChildren()
        {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;
            foreach (var child in node.Children)
            {
                Assert.True(child.IsSelected == true);
            }

            // Act
            node.IsSelected = false;

            // Assert
            foreach (var child in node.Children)
            {
                Assert.True(child.IsSelected == false);
            }
        }

        [Fact]
        public void UnselectParentWillUnselectAllChildrenExceptTheDisabledOnes()
        {
            // Arrange
            ProjectNode[] children = new ProjectNode[3];

            // make two children enabled, one disabled
            for (int i = 0; i < 3; i++)
            {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project)
                {
                    IsSelected = true,
                    IsEnabled = i % 2 == 0,
                };
                children[i] = node;
            }

            var folder = new FolderNode(null, "A", children);

            Assert.True(children[0].IsSelected == true);
            Assert.True(children[1].IsSelected == true);
            Assert.True(children[2].IsSelected == true);

            // Act
            folder.IsSelected = false;

            // Assert
            Assert.True(children[0].IsSelected == false);
            Assert.True(children[1].IsSelected == true);
            Assert.True(children[2].IsSelected == false);
        }

        [Fact]
        public void SelectAllChildrenWillAlsoSelectParent()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act
            foreach (var child in node.Children)
            {
                child.IsSelected = true;
            }

            // Assert
            Assert.True(node.IsSelected == true);
        }

        [Fact]
        public void UnselectAllChildrenWillAlsoUnselectParent()
        {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;

            // Act
            foreach (var child in node.Children)
            {
                child.IsSelected = false;
            }

            // Assert
            Assert.True(node.IsSelected == false);
        }

        [Fact]
        public void SelectOneOfTheChildrenWillChangeParentToUnderminateState()
        {
            // Arrange
            var node = CreateFolderNode();

            // Act
            var firstNode = node.Children.First();
            firstNode.IsSelected = true;

            // Assert
            Assert.True(node.IsSelected == null);
        }

        [Fact]
        public void UnselectOneOfTheChildrenWillChangeParentToUnderminateState()
        {
            // Arrange
            var node = CreateFolderNode();
            node.IsSelected = true;

            // Act
            var firstNode = node.Children.First();
            firstNode.IsSelected = false;

            // Assert
            Assert.True(node.IsSelected == null);
        }

        [Fact]
        public void ParentFolderDoNotPropagateStateBackToChildrenWhenTheyAreFirstAddedToFolder()
        {
            // Arrange
            ProjectNode[] children = new ProjectNode[3];

            // make two children selected, one unselected
            for (int i = 0; i < 3; i++)
            {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project)
                {
                    IsSelected = i % 2 == 0
                };
                children[i] = node;
            }

            var folder = new FolderNode(null, "A", children);

            // Act
            var root = new FolderNode(null, "Root", new[] { folder });

            // Assert
            Assert.Null(folder.IsSelected);
            for (int i = 0; i < 3; i++)
            {
                Assert.True(children[0].IsSelected == true);
                Assert.True(children[1].IsSelected == false);
                Assert.True(children[2].IsSelected == true);
            }
        }

        [Fact]
        public void ParentNodeIsDisabledIfAllChildrenAreDisabled()
        {
            // Arrange
            var children = new ProjectNode[3];

            // disable all children
            for (int i = 0; i < 3; i++)
            {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project)
                {
                    IsEnabled = false
                };
                children[i] = node;
            }

            // Act
            var folder = new FolderNode(null, "A", children);

            // Assert
            Assert.False(folder.IsEnabled);
        }

        [Fact]
        public void ParentNodeIsEnabledIfAtLeastOneChildrenIsEnabled()
        {
            // Arrange
            var children = new ProjectNode[3];

            // make two children enabled, one disabled
            for (int i = 0; i < 3; i++)
            {
                var project = MockProjectUtility.CreateMockProject("p" + i);
                var node = new ProjectNode(project)
                {
                    IsEnabled = i % 2 == 0
                };
                children[i] = node;
            }

            // Act
            var folder = new FolderNode(null, "A", children);

            // Assert
            Assert.True(folder.IsEnabled);
        }

        private FolderNode CreateFolderNode(string name = "A", ICollection<ProjectNodeBase> children = null)
        {
            if (children == null)
            {
                children = new List<ProjectNodeBase>();

                for (int i = 0; i < 3; i++)
                {
                    var project = MockProjectUtility.CreateMockProject("p" + i);
                    var node = new ProjectNode(project);
                    children.Add(node);
                }
            }

            return new FolderNode(null, name, children);
        }
    }
}
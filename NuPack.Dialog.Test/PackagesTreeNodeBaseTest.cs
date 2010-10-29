using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class PackagesTreeNodeBaseTest {

        [TestMethod]
        public void ParentPropertyIsCorrect() {
            // Arrange
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(parentTreeNode);

            // Act & Assert
            Assert.AreSame(parentTreeNode, node.Parent);
        }

        [TestMethod]
        public void IsSearchResultsNodeIsFalse() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.IsFalse(node.IsSearchResultsNode);
        }

        [TestMethod]
        public void IsExpandedIsFalseByDefault() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.IsFalse(node.IsExpanded);
        }

        [TestMethod]
        public void IsSelectedIsFalseByDefault() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.IsFalse(node.IsSelected);
        }

        [TestMethod]
        public void ExtensionsPropertyIsNotNull() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.IsNotNull(node.Extensions);
        }

        [TestMethod]
        public void NodesPropertyIsNotNullAndEmpty() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.IsNotNull(node.Nodes);
            Assert.AreEqual(0, node.Nodes.Count);
        }

        [TestMethod]
        public void ToStringMethodReturnsName() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.AreEqual("Mock Tree Node", node.ToString());
        }

        [TestMethod]
        public void TotalPagesPropertyIsCorrect() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.AreEqual(1, node.TotalPages);
        }

        [TestMethod]
        public void CurrentPagesPropertyIsCorrect() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.AreEqual(1, node.CurrentPage);
        }

        [TestMethod]
        public void IsSelectedPropertyWhenChangedRaiseEvent() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            node.IsSelected = false;

            bool propertyRaised = false;
            node.PropertyChanged += (o, e) => {
                if (e.PropertyName == "IsSelected") {
                    propertyRaised = true;
                }
            };

            // Act
            node.IsSelected = true;

            // Assert
            Assert.IsTrue(propertyRaised);
        }

        [TestMethod]
        public void IsExpandedPropertyWhenChangedRaiseEvent() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            node.IsExpanded = false;

            bool propertyRaised = false;
            node.PropertyChanged += (o, e) => {
                if (e.PropertyName == "IsExpanded") {
                    propertyRaised = true;
                }
            };

            // Act
            node.IsExpanded = true;

            // Assert
            Assert.IsTrue(propertyRaised);
        }

        [TestMethod]
        public void WhenConstructedLoadPageOneAutomatically() {
            int numberOfPackages = 10;

            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(null, numberOfPackages);

            node.QueryExecutionCallback = delegate {
                // Assert
                Assert.AreEqual(1, node.TotalPages);
                Assert.AreEqual(10, node.Extensions.Count);

                resetEvent.Set();
            };

            // Act
            // simply accessing the Extentions property will trigger loadding the first page
            IList<IVsExtension> extentions = node.Extensions;
            resetEvent.Wait();
        }

        [TestMethod]
        public void LoadPageMethodLoadTheCorrectExtensions() {
            // Arrange
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

            int numberOfPackages = 23;
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(null, numberOfPackages);

            node.QueryExecutionCallback = delegate {
                // Assert
                Assert.AreEqual(3, node.TotalPages);
                Assert.AreEqual(2, node.CurrentPage);
                Assert.AreEqual(10, node.Extensions.Count);

                // the loaded extensions should be from 10 to 19 (because they are on page 2)
                IList<IVsExtension> extentions = node.Extensions;
                for (int i = 0; i < 10; i++) {
                    Assert.AreEqual("A" + (i + 10), extentions[i].Name);
                }

                resetEvent.Set();
            };

            // Act
            node.LoadPage(2);
            resetEvent.Wait();
        }

        [TestMethod]
        public void LoadPageThrowsIfPageNumberIsLessThanOne() {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            ExceptionAssert.ThrowsArgOutOfRange(() => node.LoadPage(0), "pageNumber", 1, null, true);
        }

        private static PackagesTreeNodeBase CreatePackagesTreeNodeBase(IVsExtensionsTreeNode parentTreeNode = null, int numberOfPackages = 1) {
            if (parentTreeNode == null) {
                parentTreeNode = parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            }

            PackagesProviderBase provider = new MockPackagesProvider();
            return new MockTreeNode(parentTreeNode, provider, numberOfPackages);
        }
    }
}
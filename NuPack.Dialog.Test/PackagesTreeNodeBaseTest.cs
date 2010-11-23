using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            TreeNodeActionTest(node => {
                // Act
                // Simply accessing the Extentions property will trigger loadding the first page
                IList<IVsExtension> extentions = node.Extensions;
            },
            node => {
                // Assert
                Assert.AreEqual(1, node.TotalPages);
                Assert.AreEqual(10, node.Extensions.Count);
            });
        }

        [TestMethod]
        public void LoadPageMethodLoadTheCorrectExtensions() {
            TreeNodeActionTest(node => node.LoadPage(2),
                               node => {
                                   // Assert
                                   Assert.AreEqual(5, node.TotalPages);
                                   Assert.AreEqual(2, node.CurrentPage);
                                   Assert.AreEqual(2, node.Extensions.Count);

                                   Assert.AreEqual("A7", node.Extensions[0].Name);
                                   Assert.AreEqual("A6", node.Extensions[1].Name);
                               },
                               pageSize: 2,
                               numberOfPackages: 10);
        }

        [TestMethod]
        public void LoadPageMethodWithCustomSortLoadsExtensionsInTheCorrectOrder() {
            // Arrange
            var idSortDescriptor = new PackageSortDescriptor("Id", "Id", ListSortDirection.Descending);

            TreeNodeActionTest(node => node.SortSelectionChanged(idSortDescriptor),
                               node => {
                                   // Assert
                                   Assert.AreEqual(1, node.TotalPages);
                                   Assert.AreEqual(1, node.CurrentPage);
                                   Assert.AreEqual(5, node.Extensions.Count);

                                   Assert.AreEqual("A4", node.Extensions[0].Name);
                                   Assert.AreEqual("A3", node.Extensions[1].Name);
                                   Assert.AreEqual("A2", node.Extensions[2].Name);
                                   Assert.AreEqual("A1", node.Extensions[3].Name);
                                   Assert.AreEqual("A0", node.Extensions[4].Name);
                               },
                               numberOfPackages: 5);
        }

        private static void TreeNodeActionTest(Action<PackagesTreeNodeBase> treeNodeAction,
                                               Action<PackagesTreeNodeBase> callback,
                                               int? pageSize = null,
                                               int? numberOfPackages = null) {
            // Arrange
            const int defaultNumberOfPackages = 10;
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(initialState: false);
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(null, numberOfPackages ?? defaultNumberOfPackages);
            node.PageSize = pageSize ?? node.PageSize;

            Exception exception = null;

            node.QueryExecutionCallback = delegate {
                try {
                    // Callback for assertion
                    callback(node);
                }
                catch (Exception e) {
                    // There was an exception when running the callback async, so record the exception
                    exception = e;
                }
                finally {
                    // If there is an exception we don't want to freeze the unit test forever
                    resetEvent.Set();
                }
            };

            // Act
            treeNodeAction(node);

            // Wait for the event to get signaled
            resetEvent.Wait();

            // Make sure there was no exception
            Assert.IsNull(exception, exception != null ? exception.Message : String.Empty);
        }

        [TestMethod]
        public void SortSelectionChangedReturnsFalseIfCurrentSortDescriptorIsNull() {
            // Arrange
            int numberOfPackages = 10;
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(null, numberOfPackages);

            // Act
            bool result = node.SortSelectionChanged(null);

            // Assert
            Assert.IsFalse(result);
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
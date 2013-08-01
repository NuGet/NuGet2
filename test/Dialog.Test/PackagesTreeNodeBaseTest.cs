using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using Xunit;

namespace NuGet.Dialog.Test
{
    public class PackagesTreeNodeBaseTest
    {
        [Fact]
        public void ParentPropertyIsCorrect()
        {
            // Arrange
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(parentTreeNode);

            // Act & Assert
            Assert.Same(parentTreeNode, node.Parent);
        }

        [Fact]
        public void IsSearchResultsNodeIsFalse()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.False(node.IsSearchResultsNode);
        }

        [Fact]
        public void IsExpandedIsFalseByDefault()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.False(node.IsExpanded);
        }

        [Fact]
        public void IsSelectedIsFalseByDefault()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.False(node.IsSelected);
        }

        [Fact]
        public void ExtensionsPropertyIsNotNull()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.NotNull(node.Extensions);
        }

        [Fact]
        public void NodesPropertyIsNotNullAndEmpty()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.NotNull(node.Nodes);
            Assert.Equal(0, node.Nodes.Count);
        }

        [Fact]
        public void ToStringMethodReturnsName()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.Equal("Mock Tree Node", node.ToString());
        }

        [Fact]
        public void TotalPagesPropertyIsCorrect()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.Equal(1, node.TotalPages);
        }

        [Fact]
        public void CurrentPagesPropertyIsCorrect()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            Assert.Equal(1, node.CurrentPage);
        }

        [Fact]
        public void IsSelectedPropertyWhenChangedRaiseEvent()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            node.IsSelected = false;

            bool propertyRaised = false;
            node.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "IsSelected")
                {
                    propertyRaised = true;
                }
            };

            // Act
            node.IsSelected = true;

            // Assert
            Assert.True(propertyRaised);
        }

        [Fact]
        public void SearchNodeIsRemoveWhenDeselected()
        {
            // Arrange
            var parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;

            PackagesProviderBase provider = new MockPackagesProvider();
            var node = new MockTreeNode(parentTreeNode, provider, 1, true, true);
            provider.ExtensionsTree.Nodes.Add(node);
            provider.SelectedNode = node;

            var searchNode = (PackagesTreeNodeBase)provider.Search("hello");
            Assert.True(searchNode.IsSelected);
            Assert.True(searchNode.IsSearchResultsNode);

            // Act 
            searchNode.OnClosed();

            // Arrange
            Assert.Equal(node, provider.SelectedNode);
            Assert.True(node.IsSelected);
        }

        [Fact]
        public void IsExpandedPropertyWhenChangedRaiseEvent()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            node.IsExpanded = false;

            bool propertyRaised = false;
            node.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "IsExpanded")
                {
                    propertyRaised = true;
                }
            };

            // Act
            node.IsExpanded = true;

            // Assert
            Assert.True(propertyRaised);
        }

        [Fact]
        public void WhenConstructedLoadPageOneAutomatically()
        {
            TreeNodeActionTest(node =>
            {
                // Act
                // Simply accessing the Extensions property will trigger loading the first page
                IList<IVsExtension> extensions = node.Extensions;
            },
            node =>
            {
                // Assert
                Assert.Equal(1, node.TotalPages);
                Assert.Equal(10, node.Extensions.Count);
            });
        }

        [Fact]
        public void LoadPageMethodLoadTheCorrectExtensions()
        {
            TreeNodeActionTest(node => node.LoadPage(2),
                               node =>
                               {
                                   // Assert
                                   Assert.Equal(5, node.TotalPages);
                                   Assert.Equal(2, node.CurrentPage);
                                   Assert.Equal(2, node.Extensions.Count);

                                   Assert.Equal("A7", node.Extensions[0].Name);
                                   Assert.Equal("A6", node.Extensions[1].Name);

                                   Assert.Equal(10, node.TotalNumberOfPackages);
                               },
                               pageSize: 2,
                               numberOfPackages: 10);
        }

        [Fact]
        public void LoadPageMethodWithCustomSortLoadsExtensionsInTheCorrectOrder()
        {
            // Arrange
            var idSortDescriptor = new PackageSortDescriptor("Id", "Id", ListSortDirection.Descending);

            TreeNodeActionTest(node => node.SortSelectionChanged(idSortDescriptor),
                               node =>
                               {
                                   // Assert
                                   Assert.Equal(1, node.TotalPages);
                                   Assert.Equal(1, node.CurrentPage);
                                   Assert.Equal(5, node.Extensions.Count);

                                   Assert.Equal("A4", node.Extensions[0].Name);
                                   Assert.Equal("A3", node.Extensions[1].Name);
                                   Assert.Equal("A2", node.Extensions[2].Name);
                                   Assert.Equal("A1", node.Extensions[3].Name);
                                   Assert.Equal("A0", node.Extensions[4].Name);

                                   Assert.Equal(5, node.TotalNumberOfPackages);
                               },
                               numberOfPackages: 5);
        }

        [Fact]
        public void LoadPageFollowedBySortClearsCacheAndUsesNewSortOrder()
        {
            // Arrange
            var idSortDescriptor = new PackageSortDescriptor("Id", "Id", ListSortDirection.Ascending);
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(numberOfPackages: 5);
            node.IsSelected = true;

            // Act            
            TreeNodeActionTest(node,
                               n => n.LoadPage(1),
                               n =>
                               {
                                   // Assert
                                   Assert.Equal(1, n.TotalPages);
                                   Assert.Equal(1, n.CurrentPage);
                                   Assert.Equal(5, n.Extensions.Count);
                                   Assert.Equal("A4", n.Extensions[0].Name);
                                   Assert.Equal("A3", n.Extensions[1].Name);
                                   Assert.Equal("A2", n.Extensions[2].Name);
                                   Assert.Equal("A1", n.Extensions[3].Name);
                                   Assert.Equal("A0", n.Extensions[4].Name);
                               });

            TreeNodeActionTest(node,
                               n => n.SortSelectionChanged(idSortDescriptor),
                               n =>
                               {
                                   // Assert
                                   Assert.Equal(1, n.TotalPages);
                                   Assert.Equal(1, n.CurrentPage);
                                   Assert.Equal(5, n.Extensions.Count);
                                   Assert.Equal("A0", n.Extensions[0].Name);
                                   Assert.Equal("A1", n.Extensions[1].Name);
                                   Assert.Equal("A2", n.Extensions[2].Name);
                                   Assert.Equal("A3", n.Extensions[3].Name);
                                   Assert.Equal("A4", n.Extensions[4].Name);
                               });
        }

        [Fact]
        public void DuplicateExtensionsAreRemoved()
        {
            // Arrange
            var node = CreatePackagesTreeNodeBase(new[]{
                PackageUtility.CreatePackage("A", "1.0", downloadCount: 1),
                PackageUtility.CreatePackage("A", "2.0", downloadCount: 1),
                PackageUtility.CreatePackage("A", "3.0", downloadCount: 1),
                PackageUtility.CreatePackage("B", "1.0", downloadCount: 2),
                PackageUtility.CreatePackage("B", "2.0", downloadCount: 2),
                PackageUtility.CreatePackage("C", "4.0", downloadCount: 3)
            });

            // Act
            TreeNodeActionTest(node,
                               n => n.LoadPage(1),
                               n =>
                               {
                                   // Assert
                                   Assert.Equal(1, n.TotalPages);
                                   Assert.Equal(1, n.CurrentPage);
                                   Assert.Equal(3, n.Extensions.Count); 
                                   Assert.Equal("C", n.Extensions[0].Name);
                                   Assert.Equal("B", n.Extensions[1].Name);
                                   Assert.Equal("A", n.Extensions[2].Name);
                               });
        }

        [Fact]
        public void DuplicateExtensionsAreNotRemovedIfCollapseVersionsPropertyIsFalse()
        {
            // Arrange
            var node = CreatePackagesTreeNodeBase(new[]{
                    PackageUtility.CreatePackage("A", "1.0", downloadCount: 1),
                    PackageUtility.CreatePackage("A", "2.0", downloadCount: 1),
                    PackageUtility.CreatePackage("A", "3.0", downloadCount: 1),
                    PackageUtility.CreatePackage("B", "1.0", downloadCount: 2),
                    PackageUtility.CreatePackage("B", "2.0", downloadCount: 2),
                    PackageUtility.CreatePackage("C", "4.0", downloadCount: 3)
                },
                parentTreeNode: null,
                collapseVersions: false);

            // Act
            TreeNodeActionTest(node,
                               n => n.LoadPage(1),
                               n =>
                               {
                                   // Assert
                                   Assert.Equal(1, n.TotalPages);
                                   Assert.Equal(1, n.CurrentPage);
                                   Assert.Equal(6, n.Extensions.Count);
                                   Assert.Equal("C", n.Extensions[0].Name);
                                   Assert.Equal("B", n.Extensions[1].Name);
                                   Assert.Equal("B", n.Extensions[2].Name);
                                   Assert.Equal("A", n.Extensions[3].Name);
                                   Assert.Equal("A", n.Extensions[4].Name);
                                   Assert.Equal("A", n.Extensions[5].Name);
                               });
        }

        [Fact]
        public void PrereleasePackagesAreNotLoadedIfSupportsPrereleaseIsFalse()
        {
            // Arrange
            var node = CreatePackagesTreeNodeBase(new[]{
                    PackageUtility.CreatePackage("A", "1.0-alpha", downloadCount: 1),
                    PackageUtility.CreatePackage("A", "2.0", downloadCount: 1),
                    PackageUtility.CreatePackage("A", "3.0", downloadCount: 1),
                    PackageUtility.CreatePackage("B", "1.0-beta", downloadCount: 2),
                    PackageUtility.CreatePackage("B", "2.0", downloadCount: 2),
                    PackageUtility.CreatePackage("C", "4.0", downloadCount: 3)
                },
                parentTreeNode: null,
                collapseVersions: true,
                supportsPrereleasePackages: false);

            // Act
            TreeNodeActionTest(node,
                               n => n.LoadPage(1),
                               n =>
                               {
                                   // Assert
                                   Assert.Equal(1, n.TotalPages);
                                   Assert.Equal(1, n.CurrentPage);
                                   Assert.Equal(3, n.Extensions.Count);
                                   Assert.Equal("C", n.Extensions[0].Name);
                                   Assert.Equal("4.0", ((PackageItem)n.Extensions[0]).Version);
                                   
                                   Assert.Equal("B", n.Extensions[1].Name);
                                   Assert.Equal("2.0", ((PackageItem)n.Extensions[1]).Version);
                                   
                                   Assert.Equal("A", n.Extensions[2].Name);
                                   Assert.Equal("3.0", ((PackageItem)n.Extensions[2]).Version);
                               });
        }

        private static void TreeNodeActionTest(Action<PackagesTreeNodeBase> treeNodeAction,
                                               Action<PackagesTreeNodeBase> callback,
                                               int? pageSize = null,
                                               int? numberOfPackages = null)
        {
            const int defaultNumberOfPackages = 10;
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(numberOfPackages: numberOfPackages ?? defaultNumberOfPackages);
            node.IsSelected = true;
            TreeNodeActionTest(node, treeNodeAction, callback, pageSize);
        }

        private static void TreeNodeActionTest(PackagesTreeNodeBase node,
                                               Action<PackagesTreeNodeBase> treeNodeAction,
                                               Action<PackagesTreeNodeBase> callback,
                                               int? pageSize = null)
        {
            // Arrange
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(initialState: false);
            node.PageSize = pageSize ?? node.PageSize;

            Exception exception = null;

            node.PackageLoadCompleted += delegate
            {
                try
                {
                    // Callback for assertion
                    callback(node);
                }
                catch (Exception e)
                {
                    // There was an exception when running the callback async, so record the exception
                    exception = e;
                }
                finally
                {
                    // If there is an exception we don't want to freeze the unit test forever
                    resetEvent.Set();
                }
            };

            // Act
            treeNodeAction(node);

            // Wait for the event to get signaled
            resetEvent.Wait();

            // Make sure there was no exception
            Assert.Null(exception);
        }

        [Fact]
        public void SortSelectionChangedReturnsFalseIfCurrentSortDescriptorIsNull()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase(numberOfPackages: 10);

            // Act
            bool result = node.SortSelectionChanged(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LoadPageThrowsIfPageNumberIsLessThanOne()
        {
            // Arrange
            PackagesTreeNodeBase node = CreatePackagesTreeNodeBase();

            // Act & Assert
            ExceptionAssert.ThrowsArgOutOfRange(() => node.LoadPage(0), "pageNumber", 1, null, true);
        }

        private static PackagesTreeNodeBase CreatePackagesTreeNodeBase(
            IVsExtensionsTreeNode parentTreeNode = null, 
            int numberOfPackages = 1, 
            bool collapseVersions = true,
            bool supportsPrereleasePackages = true)
        {
            if (parentTreeNode == null)
            {
                parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            }

            PackagesProviderBase provider = new MockPackagesProvider();
            return new MockTreeNode(parentTreeNode, provider, numberOfPackages, collapseVersions, supportsPrereleasePackages);

        }

        private static PackagesTreeNodeBase CreatePackagesTreeNodeBase(
            IEnumerable<IPackage> packages, 
            IVsExtensionsTreeNode parentTreeNode = null, 
            bool collapseVersions = true,
            bool supportsPrereleasePackages = true)
        {
            if (parentTreeNode == null)
            {
                parentTreeNode = parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            }

            PackagesProviderBase provider = new MockPackagesProvider();
            provider.IncludePrerelease = true;
            return new MockTreeNode(parentTreeNode, provider, packages, collapseVersions, supportsPrereleasePackages)
            {
                IsSelected = true
            };
        }
    }
}
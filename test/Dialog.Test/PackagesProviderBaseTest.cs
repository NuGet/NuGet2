using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.Dialog.Test
{
    public class PackagesProviderBaseTest
    {
        [Fact]
        public void CtorThrowsIfResourcesArgumentIsNull()
        {
            ExceptionAssert.ThrowsArgNull(
                () => new ConcretePackagesProvider(null),
                "resources");
        }

        [Fact]
        public void ToStringMethodReturnsNameValue()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // act
            string providerName = provider.ToString();

            // Assert
            Assert.Equal("Test Provider", providerName);
        }

        [Fact]
        public void PropertyRefreshOnNodeSelectionIsFalse()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act & Assert
            Assert.False(provider.RefreshOnNodeSelection);
        }

        [Fact]
        public void ExtensionsTreeIsNotNull()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.NotNull(provider.ExtensionsTree);
        }

        [Fact]
        public void ExtensionsTreeIsPopulatedWithOneNode()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.Equal(1, provider.ExtensionsTree.Nodes.Count);
            Assert.IsType(typeof(SimpleTreeNode), provider.ExtensionsTree.Nodes[0]);
            Assert.Equal("All", provider.ExtensionsTree.Nodes[0].Name);
        }

        [Fact]
        public void SearchMethodCreatesNewTreeNode()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();
            provider.SelectedNode = (PackagesTreeNodeBase)provider.ExtensionsTree.Nodes[0];

            // Act
            IVsExtensionsTreeNode searchNode = provider.Search("hello");

            // Assert
            Assert.NotNull(searchNode);
            Assert.IsType(typeof(PackagesSearchNode), searchNode);
            Assert.True(provider.ExtensionsTree.Nodes.Contains(searchNode));
        }

        [Fact]
        public void SearchMethodDoNotCreateNewSearchNodeWhenSearchTextChanges()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();
            provider.SelectedNode = (PackagesTreeNodeBase)provider.ExtensionsTree.Nodes[0];

            // Act
            IVsExtensionsTreeNode searchNode = provider.Search("hello");
            IVsExtensionsTreeNode secondSearchNode = provider.Search("hellop");

            // Assert
            Assert.Same(searchNode, secondSearchNode);
        }

        [Fact]
        public void AfterSearchIsDoneTheOriginalNodeIsResetToTheFirstPage()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();
            provider.SelectedNode = (PackagesTreeNodeBase)provider.ExtensionsTree.Nodes[0];

            // Act
            provider.SelectedNode.CurrentPage = 2;

            // Assert
            Assert.Equal(2, provider.SelectedNode.CurrentPage);

            // Act
            provider.Search("hello");
            // clear the search
            provider.Search("");

            // Assert
            Assert.NotNull(provider.SelectedNode);
            Assert.Equal(1, provider.SelectedNode.CurrentPage);
        }

        [Fact]
        public void SearchMethodReturnsNullForNullOrEmptySearchText()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();
            provider.SelectedNode = (PackagesTreeNodeBase)provider.ExtensionsTree.Nodes[0];

            // Act
            IVsExtensionsTreeNode searchNode = provider.Search(null);
            IVsExtensionsTreeNode secondSearchNode = provider.Search("");

            // Assert
            Assert.Null(searchNode);
            Assert.Null(secondSearchNode);
        }

        [Fact]
        public void MediumIconDataTemplate()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.NotNull(provider.MediumIconDataTemplate);
        }

        [Fact]
        public void DetailViewDataTemplate()
        {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.NotNull(provider.DetailViewDataTemplate);
        }

        private PackagesProviderBase CreatePackagesProviderBase()
        {
            ResourceDictionary resources = new ResourceDictionary();
            resources.Add("PackageItemTemplate", new DataTemplate());
            resources.Add("PackageDetailTemplate", new DataTemplate());

            return new ConcretePackagesProvider(resources);
        }

        private class ConcretePackagesProvider : PackagesProviderBase
        {
            public ConcretePackagesProvider(ResourceDictionary resources) :
                this(new Mock<IPackageRepository>().Object, resources)
            {
            }

            public ConcretePackagesProvider(IPackageRepository packageRepository, ResourceDictionary resources) :
                base(
                    packageRepository,
                    resources,
                    new ProviderServices(
                       new Mock<IUserNotifierServices>().Object,
                       new Mock<IProgressWindowOpener>().Object,
                       new Mock<IProviderSettings>().Object,
                       new Mock<IUpdateAllUIService>().Object,
                       new Mock<IScriptExecutor>().Object,
                       new MockOutputConsoleProvider(),
                       new Mock<IVsCommonOperations>().Object), 
                       new Mock<IProgressProvider>().Object, 
                       new Mock<ISolutionManager>().Object)
            {
            }

            public override IVsExtension CreateExtension(IPackage package)
            {
                return new Mock<IVsExtension>().Object;
            }

            public override bool CanExecute(PackageItem item)
            {
                return false;
            }

            public override void Execute(PackageItem item)
            {
            }

            protected override void FillRootNodes()
            {
                var repository = new MockPackageRepository();
                repository.AddPackage(PackageUtility.CreatePackage("hello", "1.0"));
                repository.AddPackage(PackageUtility.CreatePackage("world", "2.0"));
                repository.AddPackage(PackageUtility.CreatePackage("nuget", "3.0"));

                RootNode.Nodes.Add(new SimpleTreeNode(this, "All", RootNode, repository));
            }

            public override string Name
            {
                get
                {
                    return "Test Provider";
                }
            }
        }
    }
}
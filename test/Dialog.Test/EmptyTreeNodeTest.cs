using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Dialog.Test
{
    public class EmptyTreeNodeTest
    {
        [Fact]
        public void PropertyNameIsCorrect()
        {
            // Arrange
            MockPackageRepository repository = new MockPackageRepository();

            string category = "Mock node";
            EmptyTreeNode node = CreateEmptyTreeNode(category);

            // Act & Assert
            Assert.Equal(category, node.Name);
        }

        [Fact]
        public void GetPackagesReturnCorrectPackages()
        {
            // Arrange
            EmptyTreeNode node = CreateEmptyTreeNode();

            // Act
            var producedPackages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true);

            // Assert
            Assert.Empty(producedPackages);
        }

        private static EmptyTreeNode CreateEmptyTreeNode(string category = "Mock node")
        {
            PackagesProviderBase provider = new MockPackagesProvider();
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new EmptyTreeNode(provider, category, parentTreeNode);
        }
    }
}

using System;
using Microsoft.VisualStudio.Shell;
using Moq;
using NuGet.Test;
using Xunit;
using System.Globalization;

namespace NuGet.VisualStudio11.Test
{
    public class NuGetStaticSearchResultTest
    {
        private const string DummyText = "Entity";

        public NuGetStaticSearchResultTest()
        {
            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.EnvironmentPermissionAttribute));
        }

        [Fact]
        public void NuGetStaticSearchResultCtorNullParameterThrows()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);

            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new NuGetStaticSearchResult(null, searchProvider, managePackageCommand.Object), "searchText");
            ExceptionAssert.ThrowsArgNull(() => new NuGetStaticSearchResult(DummyText, null, managePackageCommand.Object), "provider");
            ExceptionAssert.ThrowsArgNull(() => new NuGetStaticSearchResult(DummyText, searchProvider, null), "supportedManagePackageCommand");
        }

        [Fact]
        public void NuGetStaticSearchResultReturnNonNull()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);
            NuGetStaticSearchResult result = new NuGetStaticSearchResult(DummyText, searchProvider, managePackageCommand.Object);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void NuGetStaticSearchResultValidatePublicPropertiesGet()
        {
            // Arrange & Act
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);
            NuGetStaticSearchResult result = new NuGetStaticSearchResult(DummyText, searchProvider, managePackageCommand.Object);

            // Assert
            Assert.Null(result.Description);
            Assert.Equal(String.Format("Search Online for NuGet Packages matching '{0}'", DummyText), result.DisplayText);
            // Uri scheme is not registered in the unit test environment. And, registration of urischeme 'pack' and prefix 'application'
            // will require loading of System.Windows.Application which will load multiple assemblies and classes and slow things down
            // Hence, ignoring testing of icon
            Assert.Null(result.Icon);
            Assert.Null(result.PersistenceData); // Most Recently Used (MRU) data is not stored for search results of type NuGetStaticSearchResult
            Assert.Equal(searchProvider, result.SearchProvider);
            Assert.Null(result.Tooltip);
        }

        [Fact]
        public void NuGetStaticSearchResultValidateInvokeCommand()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);
            NuGetStaticSearchResult result = new NuGetStaticSearchResult(DummyText, searchProvider, managePackageCommand.Object);

            // Act
            result.InvokeAction();

            // Assert
            managePackageCommand.Verify(m => m.Invoke(DummyText + " /searchin:online"), Times.Once());
        }
    }
}

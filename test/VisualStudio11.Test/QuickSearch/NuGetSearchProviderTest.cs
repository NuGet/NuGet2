using System;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test;
using Xunit;

namespace NuGet.VisualStudio11.Test
{
    public class NuGetSearchProviderTest
    {
        private const string DummyText = "Entity";

        public NuGetSearchProviderTest()
        {
            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.EnvironmentPermissionAttribute));
        }

        [Fact]
        public void NuGetSearchProviderCtorNullParameterThrows()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);

            // Act
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchProvider(null, managePackageCommand.Object), "managePackageDialogCommand");
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchProvider(managePackageCommand.Object, null), "managePackageForSolutionDialogCommand");
        }

        [Fact]
        public void NuGetSearchProviderValidateCreateSearchNilCookie()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(managePackageCommand.Object, managePackageCommand.Object);
            Mock<IVsSearchQuery> pSearchQuery = new Mock<IVsSearchQuery>();
            Mock<IVsSearchProviderCallback> pSearchProviderCallback = new Mock<IVsSearchProviderCallback>();

            // Act
            IVsSearchTask searchTask = searchProvider.CreateSearch(dwCookie: VSConstants.VSCOOKIE_NIL, pSearchQuery: pSearchQuery.Object, pSearchCallback: pSearchProviderCallback.Object);
            // Assert
            Assert.Null(searchTask);
        }

        [Fact]
        public void NuGetSearchProviderValidateCreateSearchNonNull()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(managePackageCommand.Object, managePackageCommand.Object);
            Mock<IVsSearchQuery> pSearchQuery = new Mock<IVsSearchQuery>();
            Mock<IVsSearchProviderCallback> pSearchProviderCallback = new Mock<IVsSearchProviderCallback>();

            // Act
            IVsSearchTask searchTask = searchProvider.CreateSearch(1, pSearchQuery.Object, pSearchProviderCallback.Object);

            // Assert
            Assert.NotNull(searchTask);
        }

        [Fact]
        public void NuGetSearchProviderValidateCreateItemResult()
        {
            // Arrange
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            NuGetSearchProvider searchProvider = new NuGetSearchProvider(managePackageCommand.Object, managePackageCommand.Object);
            IVsSearchItemResult result = null;

            // Act
            result = searchProvider.CreateItemResult(DummyText);

            // Assert
            Assert.Null(result);
        }
    }
}

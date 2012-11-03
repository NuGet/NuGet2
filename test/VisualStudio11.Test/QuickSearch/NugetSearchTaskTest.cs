using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio11;
using Xunit;

namespace NuGet.VisualStudio11.Test
{
    public class NuGetSearchTaskTest
    {
        private const string DummyText = "Entity";

        public NuGetSearchTaskTest()
        {
            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.EnvironmentPermissionAttribute));
        }

        [Fact]
        public void NuGetSearchTaskCtorNullParameterThrows()
        {
            // Arrange
            Mock<IVsSearchQuery> searchQuery = new Mock<IVsSearchQuery>();
            Mock<IVsSearchProviderCallback> searchCallback = new Mock<IVsSearchProviderCallback>();
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider provider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);

            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchTask(null, 1, searchQuery.Object, searchCallback.Object, managePackageCommand.Object, managePackageCommand.Object), "provider");
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchTask(provider, 1, null, searchCallback.Object, managePackageCommand.Object, managePackageCommand.Object), "searchQuery");
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchTask(provider, 1, searchQuery.Object, null, managePackageCommand.Object, managePackageCommand.Object), "searchCallback");
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchTask(provider, 1, searchQuery.Object, searchCallback.Object, null, managePackageCommand.Object), "managePackageDialogCommand");
            ExceptionAssert.ThrowsArgNull(() => new NuGetSearchTask(provider, 1, searchQuery.Object, searchCallback.Object, managePackageCommand.Object, null), "managePackageForSolutionDialogCommand");
        }

        [Fact]
        public void NuGetSearchTaskValidateStart()
        {
            // Arrange
            Mock<IVsSearchQuery> searchQuery = new Mock<IVsSearchQuery>();
            Mock<IVsSearchProviderCallback> searchCallback = new Mock<IVsSearchProviderCallback>();
            searchCallback.Setup(s => s.ReportComplete(It.IsAny<IVsSearchTask>(), It.IsAny<uint>()));
            OleMenuCommand managePackageCommand = new OleMenuCommand(null, new System.ComponentModel.Design.CommandID(Guid.Empty, 0));
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider provider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand, managePackageCommand);
            NuGetSearchTask searchTask = new NuGetSearchTask(provider, 1, searchQuery.Object, searchCallback.Object, managePackageCommand, managePackageCommand);

            // Act
            searchTask.Start();

            // Assert
            Assert.Equal(__VSSEARCHTASKSTATUS.STS_COMPLETED, (__VSSEARCHTASKSTATUS)searchTask.Status);
        }

        [Fact]
        public void NuGetSearchTaskValidateStop()
        {
            // Arrange
            Mock<IVsSearchQuery> searchQuery = new Mock<IVsSearchQuery>();
            Mock<IVsSearchProviderCallback> searchCallback = new Mock<IVsSearchProviderCallback>();
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider provider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);
            NuGetSearchTask searchTask = new NuGetSearchTask(provider, 1, searchQuery.Object, searchCallback.Object, managePackageCommand.Object, managePackageCommand.Object);

            // Act
            searchTask.Stop();

            // Assert
            Assert.Equal((uint)__VSSEARCHTASKSTATUS.STS_STOPPED, searchTask.Status);
        }

        [Fact]
        public void NuGetSearchTaskValidatePublicPropertiesGet()
        {
            // Arrange
            var searchQuery = new Mock<IVsSearchQuery>();
            searchQuery.Setup(s => s.SearchString).Returns(DummyText);
            searchQuery.Setup(s => s.ParseError).Returns(0);
            Mock<IVsSearchProviderCallback> searchCallback = new Mock<IVsSearchProviderCallback>();
            Mock<OleMenuCommand> managePackageCommand = new Mock<OleMenuCommand>(null, null);
            Mock<OleMenuCommandService> menuCommandService = new Mock<OleMenuCommandService>(new Mock<IServiceProvider>().Object);
            NuGetSearchProvider provider = new NuGetSearchProvider(menuCommandService.Object, managePackageCommand.Object, managePackageCommand.Object);
            uint cookie = 1;

            // Act
            NuGetSearchTask searchTask = new NuGetSearchTask(provider, cookie, searchQuery.Object, searchCallback.Object, managePackageCommand.Object, managePackageCommand.Object);

            // Assert
            Assert.Equal(0 /* No Error */, searchTask.ErrorCode);
            Assert.Equal(cookie, searchTask.Id);
            Assert.Equal(DummyText, searchTask.SearchQuery.SearchString);
            Assert.Equal(0u /* No Error */, searchTask.SearchQuery.ParseError);
            Assert.Equal((uint)__VSSEARCHTASKSTATUS.STS_CREATED, searchTask.Status);
        }
    }
}

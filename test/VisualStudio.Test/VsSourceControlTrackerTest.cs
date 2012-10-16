using System;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsSourceControlTrackerTest
    {
        [Fact]
        public void ConstructorStartTrackingIfSolutionIsOpen()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var projectDocumentsEvents = new Mock<IVsTrackProjectDocuments2>();

            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true);
            solutionManager.Setup(s => s.SolutionDirectory).Returns("foo:\\bar");

            // Act
            var scTracker = new VsSourceControlTracker(
                solutionManager.Object, fileSystemProvider.Object, projectDocumentsEvents.Object, new Mock<ISettings>().Object);

            // Assert
            uint cookie;
            projectDocumentsEvents.Verify(
                p => p.AdviseTrackProjectDocumentsEvents(It.IsAny<IVsTrackProjectDocumentsEvents2>(), out cookie),
                Times.Once());
        }

        [Fact]
        public void StartTrackingIfNewSolutionIsOpen()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var projectDocumentsEvents = new Mock<IVsTrackProjectDocuments2>();
            solutionManager.Setup(s => s.IsSolutionOpen).Returns(false);
            solutionManager.Setup(s => s.SolutionDirectory).Returns("baz:\\foo");

            var fileSystem = new Mock<IFileSystem>();
            fileSystemProvider.Setup(f => f.GetFileSystem("baz:\\foo\\.nuget", It.IsAny<bool>())).Returns(fileSystem.Object);

            // Act
            var scTracker = new VsSourceControlTracker(
                solutionManager.Object, fileSystemProvider.Object, projectDocumentsEvents.Object, new Mock<ISettings>().Object);
            solutionManager.Raise(s => s.SolutionOpened += (o, e) => { }, EventArgs.Empty);

            // Assert
            uint cookie;
            projectDocumentsEvents.Verify(
                p => p.AdviseTrackProjectDocumentsEvents(It.IsAny<IVsTrackProjectDocumentsEvents2>(), out cookie),
                Times.Once());
        }

        [Fact]
        public void DoNotTrackIfNewSourceControlIntegrationIsDisabled()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var projectDocumentsEvents = new Mock<IVsTrackProjectDocuments2>();

            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true);
            solutionManager.Setup(s => s.SolutionDirectory).Returns("baz:\\foo");

            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns("true");

            // Act
            var scTracker = new VsSourceControlTracker(
                solutionManager.Object, fileSystemProvider.Object, projectDocumentsEvents.Object, settings.Object);

            // Assert
            uint cookie;
            projectDocumentsEvents.Verify(
                p => p.AdviseTrackProjectDocumentsEvents(It.IsAny<IVsTrackProjectDocumentsEvents2>(), out cookie),
                Times.Never());
        }

        [Fact]
        public void DoNotTrackWhenSolutionIsOpenButSourceControlIntegrationIsDisabled()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var projectDocumentsEvents = new Mock<IVsTrackProjectDocuments2>();

            solutionManager.Setup(s => s.IsSolutionOpen).Returns(false);
            solutionManager.Setup(s => s.SolutionDirectory).Returns("baz:\\foo");

            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("solution", "disableSourceControlIntegration")).Returns("true");

            var scTracker = new VsSourceControlTracker(
                solutionManager.Object, fileSystemProvider.Object, projectDocumentsEvents.Object, settings.Object);

            // Act
            solutionManager.Raise(s => s.SolutionOpened += (o, e) => { }, EventArgs.Empty);

            // Assert
            uint cookie;
            projectDocumentsEvents.Verify(
                p => p.AdviseTrackProjectDocumentsEvents(It.IsAny<IVsTrackProjectDocumentsEvents2>(), out cookie),
                Times.Never());
        }

        [Fact]
        public void StopTrackingWhenSolutionIsClosed()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var projectDocumentsEvents = new Mock<IVsTrackProjectDocuments2>();

            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true);
            solutionManager.Setup(s => s.SolutionDirectory).Returns("baz:\\foo");

            var fileSystem = new MockFileSystem();
            fileSystemProvider.Setup(f => f.GetFileSystem("baz:\\foo\\.nuget", It.IsAny<bool>())).Returns(fileSystem);

            var scTracker = new VsSourceControlTracker(
                solutionManager.Object, fileSystemProvider.Object, projectDocumentsEvents.Object, new Mock<ISettings>().Object);

            // Act
            solutionManager.Raise(s => s.SolutionClosed += (o, e) => { }, EventArgs.Empty);

            // Assert
            projectDocumentsEvents.Verify(
                p => p.UnadviseTrackProjectDocumentsEvents(It.IsAny<uint>()),
                Times.Once());
        }

        [Fact]
        public void RaiseEventWhenSourceControlStateChanged()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var projectDocumentsEvents = new MockIVsTrackProjectDocuments();

            solutionManager.Setup(s => s.IsSolutionOpen).Returns(true);
            solutionManager.Setup(s => s.SolutionDirectory).Returns("baz:\\foo");

            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(r => r.RepositoryPath).Returns("x:\non");
            var repositorySettingsLazy = new Lazy<IRepositorySettings>(() => repositorySettings.Object);

            var scTracker = new VsSourceControlTracker(
                solutionManager.Object, fileSystemProvider.Object, projectDocumentsEvents, new Mock<ISettings>().Object)
                                {
                                    RepositorySettings = repositorySettingsLazy
                                };

            bool eventRaised = false;
            scTracker.SolutionBoundToSourceControl += (o, e) => eventRaised = true;

            // Act
            projectDocumentsEvents.OnAfterSccStatusChanged(new Mock<IVsProject>().Object, 1, new[] {"sol.sln"},
                                                           new[] {(uint) 2});

            // Assert
            Assert.True(eventRaised);
        }
    }
}
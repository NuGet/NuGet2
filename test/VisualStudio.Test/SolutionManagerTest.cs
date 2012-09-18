using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class SolutionManagerTest
    {
        [Fact]
        public void CallDeleteMarkedPackageDirectoriesOnSolutionOpen()
        {
            // Arrange
            uint solutionLoadedUICookie = 0x12;
            var mockVsMonitorSelection = new Mock<IVsMonitorSelection>();
            Guid solutionLoadedGuid = Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid;
            mockVsMonitorSelection.Setup(m => m.GetCmdUIContextCookie(ref solutionLoadedGuid, out solutionLoadedUICookie));

            var solutionManager = new SolutionManager(GetMockDTE(), new Mock<IVsSolution>().Object, mockVsMonitorSelection.Object);
            var mockDeleteOnRestartManager = new Mock<IDeleteOnRestartManager>();
            solutionManager.DeleteOnRestartManager = new Lazy<IDeleteOnRestartManager>(() => mockDeleteOnRestartManager.Object);

            // Act
            solutionManager.OnCmdUIContextChanged(solutionLoadedUICookie, fActive: 1);

            // Assert
            mockDeleteOnRestartManager.Verify(m => m.DeleteMarkedPackageDirectories(), Times.Exactly(1));
        }

        private DTE GetMockDTE()
        {
            var dte = new Mock<DTE>();
            var dteEvents = new Mock<Events>();
            var dteSolution = new Mock<Solution>();
            dteEvents.SetupGet(m => m.SolutionEvents).Returns(new Mock<SolutionEvents>().Object);
            dteSolution.SetupGet(m => m.IsOpen).Returns(false);
            dte.SetupGet(m => m.Events).Returns(dteEvents.Object);
            dte.SetupGet(m => m.Solution).Returns(dteSolution.Object);
            return dte.Object;
        }
    }
}

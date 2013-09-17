using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VsEvents;

namespace NuGet.Tools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideBindingPath]
    // The right UIContext to use is VSConstants.UICONTEXT.SolutionBuilding_string, which 
    // works for Dev 11 and later. Unfortunately, on Dev 10, the OnSolutionBegin event is fired 
    // BEFORE our package is loaded. Thus, package restore will not work on the first build.
    // When Dev 10 is no longer supported, we should change this UICONTEXT.
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ProjectRetargeting_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOrProjectUpgrading_string)]
    [Guid(GuidList.guidNuGetVSEventsPackagePkgString)]
    public sealed class NuGetVSEventsPackage : Package
    {
        private DTEEvents _dteEvents;
        private PackageRestorer _packageRestorer;
        private ProjectRetargetingHandler _projectRetargetHandler;
        private ProjectUpgradeHandler _projectUpgradeHandler;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public NuGetVSEventsPackage()
        {
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            var dte = (DTE)GetService(typeof(SDTE));
            _dteEvents = dte.Events.DTEEvents;
            _dteEvents.OnBeginShutdown += OnBeginShutDown;

            _packageRestorer = new PackageRestorer(dte, this);
            _projectRetargetHandler = new ProjectRetargetingHandler(dte, this);
            _projectUpgradeHandler = new ProjectUpgradeHandler(this);
        }

        private void OnBeginShutDown()
        {
            _projectRetargetHandler.Dispose();
            _projectUpgradeHandler.Dispose();
            _packageRestorer.Dispose();
            _packageRestorer = null;

            _dteEvents.OnBeginShutdown -= OnBeginShutDown;
            _dteEvents = null;
        }
    }
}
using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using EnvDTE;
using System.IO;

namespace NuPack.VisualStudio.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Install, "Package")]
    public class InstallPackageCmdlet : ProcessPackageBaseCmdlet
    {
        private const string ErrorId = "Install-Package";

        #region Parameters

        [Parameter(Position=2)]
        public Version Version { get; set; }

        [Parameter(Position=3)]
        public SwitchParameter IgnoreDependencies { get; set; }

        #endregion

        protected override void ProcessRecordCore()
        {
            if (!IsSolutionOpen) {
                WriteError("There is no active solution.", ErrorId);
                return;
            }

            var packageManager = PackageManager;
            bool isSolutionLevelPackage = IsSolutionOnlyPackage(packageManager.SourceRepository, Id, Version);
            
            if (isSolutionLevelPackage) {
                if (!String.IsNullOrEmpty(Project)) {
                    WriteError(
                        String.Format(
                            CultureInfo.CurrentCulture, 
                            "The package '{0}' only applies to the solution and not to a project. Remove the -Project parameter.", 
                            Id),
                        ErrorId);
                }
                else {
                    packageManager.InstallPackage(Id, Version, IgnoreDependencies.IsPresent);
                }
            }
            else {
                var projectManager = ProjectManager;
                if (projectManager != null) {
                    projectManager.AddPackageReference(Id, Version, IgnoreDependencies.IsPresent);
                }
                else {
                    WriteError("Missing project parameter or invalid project name.", ErrorId);
                }
            }
        }
    }
}
using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using EnvDTE;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This class acts as the base class for InstallPackage, UninstallPackage and UpdatePackage commands.
    /// </summary>
    public abstract class ProcessPackageBaseCmdlet : NuPackBaseCmdlet {

        private ProjectManager _projectManager;
        protected ProjectManager ProjectManager {
            get {
                if (_projectManager == null) {
                    _projectManager = GetProjectManager(Project);
                }

                return _projectManager;
            }
        }

        #region Parameters

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public string Id { get; set; }

        [Parameter(Position = 1)]
        public string Project { get; set; }

        #endregion

        protected override void BeginProcessing() {
            base.BeginProcessing();

            if (PackageManager != null) {
                PackageManager.PackageInstalling += OnPackageInstalling;
                PackageManager.PackageInstalled += OnPackageInstalled;
            }
        }

        protected override void EndProcessing() {
            base.EndProcessing();

            if (PackageManager != null) {
                PackageManager.PackageInstalling -= OnPackageInstalling;
                PackageManager.PackageInstalled -= OnPackageInstalled;
            }

            if (_projectManager != null) {
                _projectManager.PackageReferenceAdded -= OnPackageReferenceAdded;
                _projectManager.PackageReferenceRemoving -= OnPackageReferenceRemoving;
            }

            WriteLine();
        }

        private ProjectManager GetProjectManager(string projectName) {

            if (PackageManager == null) {
                return null;
            }

            if (String.IsNullOrEmpty(projectName)) {
                projectName = DefaultProjectName;
            }

            if (String.IsNullOrEmpty(projectName)) {
                return null;
            }

            Project project = GetProjectFromName(projectName);
            if (project == null) {
                return null;
            }

            ProjectManager projectManager = PackageManager.GetProjectManager(project);
            projectManager.PackageReferenceAdded += OnPackageReferenceAdded;
            projectManager.PackageReferenceRemoving += OnPackageReferenceRemoving;

            return projectManager;
        }

        private void OnPackageInstalling(object sender, PackageOperationEventArgs e) {
            // write disclaimer text before a package is installed
            WriteDisclaimerText(e.Package);
        }

        private void OnPackageInstalled(object sender, PackageOperationEventArgs e) {
            AddToolsFolderToEnvironmentPath(e.InstallPath);
            ExecuteScript(e.InstallPath, "init.ps1", e.Package, null);
        }

        private void AddToolsFolderToEnvironmentPath(string installPath) {
            string toolsPath = Path.Combine(installPath, "tools");
            if (Directory.Exists(toolsPath)) {
                var envPath = (string)GetVariableValue("env:path");
                if (!envPath.EndsWith(";", StringComparison.OrdinalIgnoreCase)) {
                    envPath = envPath + ";";
                }
                envPath += toolsPath;

                SessionState.PSVariable.Set("env:path", envPath);
            }
        }

        private void OnPackageReferenceAdded(object sender, PackageOperationEventArgs e) {
            ProjectManager manager = (ProjectManager)sender;
            string projectName = manager.Project.ProjectName;
            EnvDTE.Project projectIns = GetProjectFromName(projectName);

            ExecuteScript(e.InstallPath, "install.ps1", e.Package, projectIns);
        }

        private void OnPackageReferenceRemoving(object sender, PackageOperationEventArgs e) {
            ProjectManager manager = (ProjectManager)sender;
            string projectName = manager.Project.ProjectName;
            EnvDTE.Project projectIns = GetProjectFromName(projectName);

            ExecuteScript(e.InstallPath, "uninstall.ps1", e.Package, projectIns);
        }

        protected void ExecuteScript(string rootPath, string scriptFileName, IPackage package, Project project) {
            string toolsPath = Path.Combine(rootPath, "tools");
            string fullPath = Path.Combine(toolsPath, scriptFileName);
            if (File.Exists(fullPath)) {
                var psVariable = SessionState.PSVariable;

                // set temp variables to pass to the script
                psVariable.Set("__rootPath", rootPath);
                psVariable.Set("__toolsPath", toolsPath);
                psVariable.Set("__package", package);
                psVariable.Set("__project", project);

                string command = "& '" + fullPath + "' $__rootPath $__toolsPath $__package $__project";
                WriteVerbose("Executing script file: " + fullPath);
                InvokeCommand.InvokeScript(command);

                // clear temp variables
                psVariable.Remove("__rootPath");
                psVariable.Remove("__toolsPath");
                psVariable.Remove("__package");
                psVariable.Remove("__project");
            }
        }

        protected void WriteDisclaimerText(IPackage package) {
            if (package.RequireLicenseAcceptance) {
                string message = String.Format(
                    CultureInfo.CurrentCulture,
                    VsResources.InstallSuccessDisclaimerText,
                    package.Id,
                    String.Join(", ", package.Authors),
                    package.LicenseUrl);

                WriteLine(message);
            }
        }

        protected bool IsSolutionOnlyPackage(IPackageRepository repository, string id, Version version = null) {
            var package = repository.FindPackage(id, null, null, version);
            return package != null && !package.HasProjectContent();
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsTemplateWizard))]
    public class VsTemplateWizard : IVsTemplateWizard
    {
        private readonly IVsPackageInstaller _installer;
        private VsTemplateWizardInstallerConfiguration _configuration;
        private Project _project;
        private ProjectItem _projectItem;

        private DTE DTE { get; set; }

        [ImportingConstructor]
        public VsTemplateWizard(IVsPackageInstaller installer)
        {
            _installer = installer;
        }

        private VsTemplateWizardInstallerConfiguration GetConfigurationFromVsTemplateFile(string vsTemplatePath)
        {
            XDocument document = LoadDocument(vsTemplatePath);

            return GetConfigurationFromXmlDocument(document, vsTemplatePath);
        }

        internal VsTemplateWizardInstallerConfiguration GetConfigurationFromXmlDocument(XDocument document, string vsTemplatePath, object vsExtensionManager = null)
        {
            IEnumerable<VsTemplateWizardPackageInfo> packages = Enumerable.Empty<VsTemplateWizardPackageInfo>();
            string repositoryPath = null;

            // Ignore XML namespaces since VS does not check them either when loading vstemplate files.
            XElement packagesElement = document.Root.ElementsNoNamespace("WizardData")
                .ElementsNoNamespace("packages")
                .FirstOrDefault();

            if (packagesElement != null)
            {
                RepositoryType repositoryType = GetRepositoryType(packagesElement);
                packages = GetPackages(packagesElement).ToList();

                if (packages.Any())
                {
                    repositoryPath = GetRepositoryPath(packagesElement, repositoryType, vsTemplatePath,
                        vsExtensionManager);
                }
            }

            return new VsTemplateWizardInstallerConfiguration(repositoryPath, packages);
        }

        private IEnumerable<VsTemplateWizardPackageInfo> GetPackages(XElement packagesElement)
        {
            var declarations = (from packageElement in packagesElement.ElementsNoNamespace("package")
                                let id = packageElement.GetOptionalAttributeValue("id")
                                let version = packageElement.GetOptionalAttributeValue("version")
                                select new { id, version }).ToList();

            SemanticVersion semVer;
            var missingOrInvalidAttributes = from declaration in declarations
                                             where
                                                 String.IsNullOrWhiteSpace(declaration.id) ||
                                                 String.IsNullOrWhiteSpace(declaration.version) ||
                                                 !SemanticVersion.TryParse(declaration.version, out semVer)
                                             select declaration;

            if (missingOrInvalidAttributes.Any())
            {
                ShowErrorMessage(
                    VsResources.TemplateWizard_InvalidPackageElementAttributes);
                throw new WizardBackoutException();
            }

            return from declaration in declarations
                   select new VsTemplateWizardPackageInfo(declaration.id, declaration.version);
        }

        private string GetRepositoryPath(XElement packagesElement, RepositoryType repositoryType, string vsTemplatePath, object vsExtensionManager)
        {
            switch (repositoryType)
            {
                case RepositoryType.Template:
                    return Path.GetDirectoryName(vsTemplatePath);
                case RepositoryType.Extension:
                    string repositoryId = packagesElement.GetOptionalAttributeValue("repositoryId");
                    if (repositoryId == null)
                    {
                        ShowErrorMessage(VsResources.TemplateWizard_MissingExtensionId);
                        throw new WizardBackoutException();
                    }


                    var extensionManagerShim = new ExtensionManagerShim(vsExtensionManager);
                    string installPath;
                    if (!extensionManagerShim.TryGetExtensionInstallPath(repositoryId, out installPath))
                    {
                        ShowErrorMessage(String.Format(VsResources.TemplateWizard_InvalidExtensionId,
                            repositoryId));
                        throw new WizardBackoutException();
                    }
                    return Path.Combine(installPath, "Packages");
            }
            // should not happen
            return null;
        }

        private RepositoryType GetRepositoryType(XElement packagesElement)
        {
            string repositoryAttributeValue = packagesElement.GetOptionalAttributeValue("repository");
            switch (repositoryAttributeValue)
            {
                case "extension":
                    return RepositoryType.Extension;
                case "template":
                case null:
                    return RepositoryType.Template;
                default:
                    ShowErrorMessage(String.Format(VsResources.TemplateWizard_InvalidRepositoryAttribute,
                        repositoryAttributeValue));
                    throw new WizardBackoutException();
            }
        }

        internal virtual XDocument LoadDocument(string path)
        {
            return XDocument.Load(path);
        }

        private void PerformPackageInstall(IVsPackageInstaller packageInstaller, Project project, string packageRepositoryPath, IEnumerable<VsTemplateWizardPackageInfo> packages)
        {
            var failedPackageErrors = new List<string>();
            foreach (var package in packages)
            {
                try
                {
                    DTE.StatusBar.Text = String.Format(CultureInfo.CurrentCulture, VsResources.TemplateWizard_PackageInstallStatus, package.Id, package.Version);
                    // TODO review parameters and installer call
                    // REVIEW is it OK to ignoreDependencies? The expectation is that the vstemplate will list all the required packages
                    // REVIEW We need to figure out if we can break IVsPackageInstaller interface by modifying it to accept a SemVer and still allow MVC 3 projects to work
                    packageInstaller.InstallPackage(packageRepositoryPath, project, package.Id, package.Version, ignoreDependencies: true);
                }
                catch (InvalidOperationException exception)
                {
                    failedPackageErrors.Add(package.Id + "." + package.Version + " : " + exception.Message);
                }
            }

            if (failedPackageErrors.Any())
            {
                var errorString = new StringBuilder();
                errorString.AppendFormat(VsResources.TemplateWizard_FailedToInstallPackage, packageRepositoryPath);
                errorString.AppendLine();
                errorString.AppendLine();
                errorString.Append(String.Join(Environment.NewLine, failedPackageErrors));
                ShowErrorMessage(errorString.ToString());
            }
        }

        private void ProjectFinishedGenerating(Project project)
        {
            _project = project;
        }

        private void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            _projectItem = projectItem;
        }

        private void RunFinished()
        {
            if (_projectItem != null && _project == null)
            {
                _project = _projectItem.ContainingProject;
            }

            Debug.Assert(_project != null);
            Debug.Assert(_configuration != null);
            if (_configuration.Packages.Any())
            {
                string repositoryPath = _configuration.RepositoryPath;
                PerformPackageInstall(_installer, _project, repositoryPath, _configuration.Packages);
            }
        }

        private void RunStarted(object automationObject, WizardRunKind runKind, object[] customParams)
        {
            if (runKind != WizardRunKind.AsNewProject && runKind != WizardRunKind.AsNewItem)
            {
                ShowErrorMessage(VsResources.TemplateWizard_InvalidWizardRunKind);
                throw new WizardBackoutException();
            }

            DTE = (DTE)automationObject;
            var vsTemplatePath = (string)customParams[0];
            _configuration = GetConfigurationFromVsTemplateFile(vsTemplatePath);
        }

        internal virtual void ShowErrorMessage(string message)
        {
            MessageHelper.ShowErrorMessage(message, VsResources.TemplateWizard_ErrorDialogTitle);
        }

        void IWizard.BeforeOpeningFile(ProjectItem projectItem)
        {
            // do nothing
        }

        void IWizard.ProjectFinishedGenerating(Project project)
        {
            ProjectFinishedGenerating(project);
        }

        void IWizard.ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            ProjectItemFinishedGenerating(projectItem);
        }

        void IWizard.RunFinished()
        {
            RunFinished();
        }

        void IWizard.RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            // TODO REVIEW alternatively could get body of WizardData element from replacementsDictionary["$wizarddata$"] instead of parsing vstemplate file.
            RunStarted(automationObject, runKind, customParams);
        }

        bool IWizard.ShouldAddProjectItem(string filePath)
        {
            // always add all project items
            return true;
        }

        private enum RepositoryType
        {
            /// <summary>
            /// Cache location relative to the template (inside the same folder as the vstemplate file)
            /// </summary>
            Template,
            /// <summary>
            /// Cache location relative to the VSIX that packages the project template
            /// </summary>
            Extension,
        }

        private class ExtensionManagerShim
        {
            private static Type _iInstalledExtensionType;
            private static Type _iVsExtensionManagerType;
            private static PropertyInfo _installPathProperty;
            private static Type _sVsExtensionManagerType;
            private static MethodInfo _tryGetInstalledExtensionMethod;
            private static bool _typesInitialized;

            private readonly object _extensionManager;

            public ExtensionManagerShim(object extensionManager)
            {
                InitializeTypes();
                _extensionManager = extensionManager ?? Package.GetGlobalService(_sVsExtensionManagerType);
            }

            private static void InitializeTypes()
            {
                if (_typesInitialized)
                {
                    return;
                }

                try
                {
                    Assembly extensionManagerAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => a.FullName.StartsWith("Microsoft.VisualStudio.ExtensionManager,"))
                        .First();
                    _sVsExtensionManagerType =
                        extensionManagerAssembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager");
                    _iVsExtensionManagerType =
                        extensionManagerAssembly.GetType("Microsoft.VisualStudio.ExtensionManager.IVsExtensionManager");
                    _iInstalledExtensionType =
                        extensionManagerAssembly.GetType("Microsoft.VisualStudio.ExtensionManager.IInstalledExtension");
                    _tryGetInstalledExtensionMethod = _iVsExtensionManagerType.GetMethod("TryGetInstalledExtension",
                        new[] { typeof(string), _iInstalledExtensionType.MakeByRefType() });
                    _installPathProperty = _iInstalledExtensionType.GetProperty("InstallPath", typeof(string));
                    if (_installPathProperty == null || _tryGetInstalledExtensionMethod == null ||
                        _sVsExtensionManagerType == null)
                    {
                        throw new Exception();
                    }

                    _typesInitialized = true;
                }
                catch
                {
                    // if any of the types or methods cannot be loaded throw an error. this indicates that some API in
                    // Microsoft.VisualStudio.ExtensionManager got changed.
                    throw new WizardBackoutException(VsResources.TemplateWizard_ExtensionManagerError);
                }
            }

            public bool TryGetExtensionInstallPath(string extensionId, out string installPath)
            {
                installPath = null;
                object[] parameters = new object[] { extensionId, null };
                bool result = (bool)_tryGetInstalledExtensionMethod.Invoke(_extensionManager, parameters);
                if (!result)
                {
                    return false;
                }
                object extension = parameters[1];
                installPath = _installPathProperty.GetValue(extension, index: null) as string;
                return true;
            }
        }
    }
}

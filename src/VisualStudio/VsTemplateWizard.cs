using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private const string RegistryKeyRoot = @"SOFTWARE\NuGet\Repository";

        private readonly IVsPackageInstaller _installer;
        private readonly IVsWebsiteHandler _websiteHandler;
        private VsTemplateWizardInstallerConfiguration _configuration;
        private DTE _dte;

        [ImportingConstructor]
        public VsTemplateWizard(IVsPackageInstaller installer, IVsWebsiteHandler websiteHandler)
        {
            _installer = installer;
            _websiteHandler = websiteHandler;
        }

        [Import]
        public Lazy<IRepositorySettings> RepositorySettings { get; set; }

        private VsTemplateWizardInstallerConfiguration GetConfigurationFromVsTemplateFile(string vsTemplatePath)
        {
            XDocument document = LoadDocument(vsTemplatePath);

            return GetConfigurationFromXmlDocument(document, vsTemplatePath);
        }

        internal VsTemplateWizardInstallerConfiguration GetConfigurationFromXmlDocument(XDocument document, string vsTemplatePath,
            object vsExtensionManager = null, IEnumerable<IRegistryKey> registryKeys = null)
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
                        vsExtensionManager, registryKeys);
                }
            }

            return new VsTemplateWizardInstallerConfiguration(repositoryPath, packages);
        }

        private IEnumerable<VsTemplateWizardPackageInfo> GetPackages(XElement packagesElement)
        {
            var declarations = (from packageElement in packagesElement.ElementsNoNamespace("package")
                                let id = packageElement.GetOptionalAttributeValue("id")
                                let version = packageElement.GetOptionalAttributeValue("version")
                                let createRefreshFilesInBin = packageElement.GetOptionalAttributeValue("createRefreshFilesInBin")
                                select new { id, version, createRefreshFilesInBin }).ToList();

            SemanticVersion semVer;
            bool createRefreshFilesInBinValue;
            var missingOrInvalidAttributes = from declaration in declarations
                                             where
                                                 String.IsNullOrWhiteSpace(declaration.id) ||
                                                 String.IsNullOrWhiteSpace(declaration.version) ||
                                                 !SemanticVersion.TryParse(declaration.version, out semVer) ||
                                                 (declaration.createRefreshFilesInBin != null &&
                                                  !Boolean.TryParse(declaration.createRefreshFilesInBin, out createRefreshFilesInBinValue))
                                             select declaration;

            if (missingOrInvalidAttributes.Any())
            {
                ShowErrorMessage(
                    VsResources.TemplateWizard_InvalidPackageElementAttributes);
                throw new WizardBackoutException();
            }

            return from declaration in declarations
                   select new VsTemplateWizardPackageInfo(
                       declaration.id,
                       declaration.version,
                       declaration.createRefreshFilesInBin != null && Boolean.Parse(declaration.createRefreshFilesInBin)
                    );
        }

        private string GetRepositoryPath(XElement packagesElement, RepositoryType repositoryType, string vsTemplatePath,
            object vsExtensionManager, IEnumerable<IRegistryKey> registryKeys)
        {
            switch (repositoryType)
            {
                case RepositoryType.Template:
                    return Path.GetDirectoryName(vsTemplatePath);

                case RepositoryType.Extension:
                    return GetExtensionRepositoryPath(packagesElement, vsExtensionManager);

                case RepositoryType.Registry:
                    return GetRegistryRepositoryPath(packagesElement, registryKeys);
            }
            // should not happen
            return null;
        }

        private string GetExtensionRepositoryPath(XElement packagesElement, object vsExtensionManager)
        {
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

        private string GetRegistryRepositoryPath(XElement packagesElement, IEnumerable<IRegistryKey> registryKeys)
        {
            // When pulling the repository from the registry, use CurrentUser first, falling back onto LocalMachine
            registryKeys = registryKeys ??
                new[] {
                            new RegistryKeyWrapper(Microsoft.Win32.Registry.CurrentUser),
                            new RegistryKeyWrapper(Microsoft.Win32.Registry.LocalMachine)
                        };

            string keyName = packagesElement.GetOptionalAttributeValue("keyName");

            if (String.IsNullOrEmpty(keyName))
            {
                ShowErrorMessage(VsResources.TemplateWizard_MissingRegistryKeyName);
                throw new WizardBackoutException();
            }

            IRegistryKey repositoryKey = null;
            string repositoryValue = null;

            if (registryKeys != null)
            {
                // Find the first registry key that supplies the necessary subkey/value
                foreach (var registryKey in registryKeys)
                {
                    repositoryKey = registryKey.OpenSubKey(RegistryKeyRoot);

                    if (repositoryKey != null)
                    {
                        repositoryValue = repositoryKey.GetValue(keyName) as string;

                        if (!String.IsNullOrEmpty(repositoryValue))
                        {
                            break;
                        }

                        repositoryKey.Close();
                    }
                }
            }

            if (repositoryKey == null)
            {
                ShowErrorMessage(String.Format(VsResources.TemplateWizard_RegistryKeyError, RegistryKeyRoot));
                throw new WizardBackoutException();
            }

            if (String.IsNullOrEmpty(repositoryValue))
            {
                ShowErrorMessage(String.Format(VsResources.TemplateWizard_InvalidRegistryValue, keyName, RegistryKeyRoot));
                throw new WizardBackoutException();
            }

            // Ensure a trailing slash so that the path always gets read as a directory
            repositoryValue = PathUtility.EnsureTrailingSlash(repositoryValue);

            return Path.GetDirectoryName(repositoryValue);
        }

        private RepositoryType GetRepositoryType(XElement packagesElement)
        {
            string repositoryAttributeValue = packagesElement.GetOptionalAttributeValue("repository");
            switch (repositoryAttributeValue)
            {
                case "extension":
                    return RepositoryType.Extension;
                case "registry":
                    return RepositoryType.Registry;
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
                    _dte.StatusBar.Text = String.Format(CultureInfo.CurrentCulture, VsResources.TemplateWizard_PackageInstallStatus, package.Id, package.Version);

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
            TemplateFinishedGenerating(project);
        }

        private void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            TemplateFinishedGenerating(projectItem.ContainingProject);
        }

        private void TemplateFinishedGenerating(Project project)
        {
            if (_configuration.Packages.Any())
            {
                string repositoryPath = _configuration.RepositoryPath;
                PerformPackageInstall(_installer, project, repositoryPath, _configuration.Packages);

                // RepositorySettings = null in unit tests
                if (RepositorySettings != null)
                {
                    CreatingRefreshFilesInBin(
                        project,
                        RepositorySettings.Value.RepositoryPath,
                        _configuration.Packages.Where(p => p.CreateRefreshFilesInBin));
                }
            }
        }

        private void CreatingRefreshFilesInBin(Project project, string repositoryPath, IEnumerable<VsTemplateWizardPackageInfo> packageInfos)
        {
            if (project.IsWebSite())
            {
                IEnumerable<PackageName> packageNames = packageInfos.Select(pi => new PackageName(pi.Id, pi.Version));
                _websiteHandler.AddRefreshFilesForReferences(project, new PhysicalFileSystem(repositoryPath), packageNames);
            }
        }

        private void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            if (runKind != WizardRunKind.AsNewProject && runKind != WizardRunKind.AsNewItem)
            {
                ShowErrorMessage(VsResources.TemplateWizard_InvalidWizardRunKind);
                throw new WizardBackoutException();
            }

            _dte = (DTE)automationObject;
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
        }

        void IWizard.RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            // TODO REVIEW alternatively could get body of WizardData element from replacementsDictionary["$wizarddata$"] instead of parsing vstemplate file.
            RunStarted(automationObject, replacementsDictionary, runKind, customParams);
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

            /// <summary>
            /// Cache location stored in the registry
            /// </summary>
            Registry,
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
                        .First(a => a.FullName.StartsWith("Microsoft.VisualStudio.ExtensionManager,"));
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
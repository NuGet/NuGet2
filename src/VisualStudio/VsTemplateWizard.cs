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
using NuGetConsole;

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
        private IVsPackageInstallerServices _packageServices;
        private IOutputConsoleProvider _consoleProvider;
        private readonly IVsCommonOperations _vsCommonOperations;
        private readonly ISolutionManager _solutionManager;

        [ImportingConstructor]
        public VsTemplateWizard(
            IVsPackageInstaller installer,
            IVsWebsiteHandler websiteHandler,
            IVsPackageInstallerServices packageServices,
            IOutputConsoleProvider consoleProvider,
            IVsCommonOperations vsCommonOperations,
            ISolutionManager solutionManager)
        {
            _installer = installer;
            _websiteHandler = websiteHandler;
            _packageServices = packageServices;
            _consoleProvider = consoleProvider;
            _vsCommonOperations = vsCommonOperations;
            _solutionManager = solutionManager;
        }

        [Import]
        public Lazy<IRepositorySettings> RepositorySettings { get; set; }

        private VsTemplateWizardInstallerConfiguration GetConfigurationFromVsTemplateFile(string vsTemplatePath)
        {
            XDocument document = LoadDocument(vsTemplatePath);

            return GetConfigurationFromXmlDocument(document, vsTemplatePath);
        }

        internal VsTemplateWizardInstallerConfiguration GetConfigurationFromXmlDocument(
            XDocument document,
            string vsTemplatePath,
            object vsExtensionManager = null,
            IEnumerable<IRegistryKey> registryKeys = null)
        {
            IList<VsTemplateWizardPackageInfo> packages = new VsTemplateWizardPackageInfo[0];
            string repositoryPath = null;
            bool isPreunzipped = false;

            // Ignore XML namespaces since VS does not check them either when loading vstemplate files.
            XElement packagesElement = document.Root.ElementsNoNamespace("WizardData")
                .ElementsNoNamespace("packages")
                .FirstOrDefault();

            if (packagesElement != null)
            {
                string isPreunzippedString = packagesElement.GetOptionalAttributeValue("isPreunzipped");
                if (!String.IsNullOrEmpty(isPreunzippedString))
                {
                    Boolean.TryParse(isPreunzippedString, out isPreunzipped);
                }

                packages = GetPackages(packagesElement).ToList();

                if (packages.Count > 0)
                {
                    RepositoryType repositoryType = GetRepositoryType(packagesElement);
                    repositoryPath = GetRepositoryPath(packagesElement, repositoryType, vsTemplatePath, vsExtensionManager, registryKeys);
                }
            }

            return new VsTemplateWizardInstallerConfiguration(repositoryPath, packages, isPreunzipped);
        }

        private IEnumerable<VsTemplateWizardPackageInfo> GetPackages(XElement packagesElement)
        {
            var declarations = (from packageElement in packagesElement.ElementsNoNamespace("package")
                                let id = packageElement.GetOptionalAttributeValue("id")
                                let version = packageElement.GetOptionalAttributeValue("version")
                                let skipAssemblyReferences = packageElement.GetOptionalAttributeValue("skipAssemblyReferences")
                                select new { id, version, skipAssemblyReferences }).ToList();

            SemanticVersion semVer;
            bool skipAssemblyReferencesValue;
            var missingOrInvalidAttributes = from declaration in declarations
                                             where
                                                 String.IsNullOrWhiteSpace(declaration.id) ||
                                                 String.IsNullOrWhiteSpace(declaration.version) ||
                                                 !SemanticVersion.TryParse(declaration.version, out semVer) ||
                                                 (declaration.skipAssemblyReferences != null &&
                                                  !Boolean.TryParse(declaration.skipAssemblyReferences, out skipAssemblyReferencesValue))
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
                       declaration.skipAssemblyReferences != null && Boolean.Parse(declaration.skipAssemblyReferences)
                    );
        }

        private string GetRepositoryPath(
            XElement packagesElement,
            RepositoryType repositoryType,
            string vsTemplatePath,
            object vsExtensionManager,
            IEnumerable<IRegistryKey> registryKeys)
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
            string keyName = packagesElement.GetOptionalAttributeValue("keyName");
            if (String.IsNullOrEmpty(keyName))
            {
                ShowErrorMessage(VsResources.TemplateWizard_MissingRegistryKeyName);
                throw new WizardBackoutException();
            }

            IRegistryKey repositoryKey = null;
            string repositoryValue = null;

            // When pulling the repository from the registry, use CurrentUser first, falling back onto LocalMachine
            registryKeys = registryKeys ??
                new[] {
                            new RegistryKeyWrapper(Microsoft.Win32.Registry.CurrentUser),
                            new RegistryKeyWrapper(Microsoft.Win32.Registry.LocalMachine)
                      };

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

        private void PerformPackageInstall(
            IVsPackageInstaller packageInstaller,
            Project project,
            VsTemplateWizardInstallerConfiguration configuration)
        {
            string repositoryPath = configuration.RepositoryPath;
            var failedPackageErrors = new List<string>();

            IPackageRepository repository = configuration.IsPreunzipped
                                                ? (IPackageRepository)new UnzippedPackageRepository(repositoryPath)
                                                : (IPackageRepository)new LocalPackageRepository(repositoryPath);

            foreach (var package in configuration.Packages)
            {
                // Does the project already have this package installed?
                if (_packageServices.IsPackageInstalled(project, package.Id))
                {
                    // If so, is it the right version?
                    if (!_packageServices.IsPackageInstalled(project, package.Id, package.Version))
                    {
                        // No? OK, write a message to the Output window and ignore this package.
                        ShowWarningMessage(String.Format(VsResources.TemplateWizard_VersionConflict, package.Id, package.Version));
                    }
                    // Yes? Just silently ignore this package!
                }
                else
                {
                    try
                    {
                        _dte.StatusBar.Text = String.Format(CultureInfo.CurrentCulture, VsResources.TemplateWizard_PackageInstallStatus, package.Id, package.Version);
                        packageInstaller.InstallPackage(repository, project, package.Id, package.Version.ToString(), ignoreDependencies: true, skipAssemblyReferences: package.SkipAssemblyReferences);
                    }
                    catch (InvalidOperationException exception)
                    {
                        failedPackageErrors.Add(package.Id + "." + package.Version + " : " + exception.Message);
                    }
                }
            }

            if (failedPackageErrors.Any())
            {
                var errorString = new StringBuilder();
                errorString.AppendFormat(VsResources.TemplateWizard_FailedToInstallPackage, repositoryPath);
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
                PerformPackageInstall(_installer, project, _configuration);

                // RepositorySettings = null in unit tests
                if (project.IsWebSite() && RepositorySettings != null)
                {
                    using (_vsCommonOperations.SaveSolutionExplorerNodeStates(_solutionManager))
                    {
                        CreateRefreshFilesInBin(
                            project,
                            RepositorySettings.Value.RepositoryPath,
                            _configuration.Packages.Where(p => p.SkipAssemblyReferences));

                        CopyNativeBinariesToBin(project, RepositorySettings.Value.RepositoryPath, _configuration.Packages);
                    }
                }
            }
        }

        private void CreateRefreshFilesInBin(Project project, string repositoryPath, IEnumerable<VsTemplateWizardPackageInfo> packageInfos)
        {
            IEnumerable<PackageName> packageNames = packageInfos.Select(pi => new PackageName(pi.Id, pi.Version));
            _websiteHandler.AddRefreshFilesForReferences(project, new PhysicalFileSystem(repositoryPath), packageNames);
        }

        private void CopyNativeBinariesToBin(Project project, string repositoryPath, IEnumerable<VsTemplateWizardPackageInfo> packageInfos)
        {
            // By convention, we copy all files under the NativeBinaries folder under package root to the bin folder of the website
            IEnumerable<PackageName> packageNames = packageInfos.Select(pi => new PackageName(pi.Id, pi.Version));
            _websiteHandler.CopyNativeBinaries(project, new PhysicalFileSystem(repositoryPath), packageNames);
        }

        private void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            if (runKind != WizardRunKind.AsNewProject && runKind != WizardRunKind.AsNewItem)
            {
                ShowErrorMessage(VsResources.TemplateWizard_InvalidWizardRunKind);
                throw new WizardBackoutException();
            }

            _dte = (DTE)automationObject;
            if (customParams.Length > 0)
            {
                var vsTemplatePath = (string)customParams[0];
                _configuration = GetConfigurationFromVsTemplateFile(vsTemplatePath);
            }

            if (replacementsDictionary != null)
            {
                AddTemplateParameters(replacementsDictionary);
            }
        }

        private void AddTemplateParameters(Dictionary<string, string> replacementsDictionary)
        {
            // add the $nugetpackagesfolder$ parameter which returns relative path to the solution's packages folder.
            // this is used by project templates to include assembly references directly inside the template project file
            // without relying on nuget to install the actual packages. 
            string targetInstallDir;
            if (replacementsDictionary.TryGetValue("$destinationdirectory$", out targetInstallDir))
            {
                string solutionRepositoryPath = null;
                if (_dte.Solution != null && _dte.Solution.IsOpen)
                {
                    solutionRepositoryPath = RepositorySettings.Value.RepositoryPath;
                }
                else
                {
                    string solutionDir = DetermineSolutionDirectory(replacementsDictionary);
                    if (!String.IsNullOrEmpty(solutionDir))
                    {
                        // If the project is a Website that is created on an Http location, 
                        // solutionDir may be an Http address, e.g. http://localhost.
                        // In that case, we have to use forward slash instead of backward one.
                        if (Uri.IsWellFormedUriString(solutionDir, UriKind.Absolute))
                        {
                            solutionRepositoryPath = PathUtility.EnsureTrailingForwardSlash(solutionDir) + NuGet.VisualStudio.RepositorySettings.DefaultRepositoryDirectory;
                        }
                        else
                        {
                            solutionRepositoryPath = Path.Combine(solutionDir, NuGet.VisualStudio.RepositorySettings.DefaultRepositoryDirectory);
                        }
                    }
                }

                if (solutionRepositoryPath != null)
                {
                    // If the project is a Website that is created on an Http location, 
                    // targetInstallDir may be an Http address, e.g. http://localhost.
                    // In that case, we have to use forward slash instead of backward one.
                    if (Uri.IsWellFormedUriString(targetInstallDir, UriKind.Absolute))
                    {
                        targetInstallDir = PathUtility.EnsureTrailingForwardSlash(targetInstallDir);
                    }
                    else
                    {
                        targetInstallDir = PathUtility.EnsureTrailingSlash(targetInstallDir);
                    }

                    replacementsDictionary["$nugetpackagesfolder$"] =
                        PathUtility.EnsureTrailingSlash(PathUtility.GetRelativePath(targetInstallDir, solutionRepositoryPath));
                }
            }

            // provide a current timpestamp (for use by universal provider)
            replacementsDictionary["$timestamp$"] = DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        internal virtual void ShowErrorMessage(string message)
        {
            MessageHelper.ShowErrorMessage(message, VsResources.TemplateWizard_ErrorDialogTitle);
        }

        internal virtual void ShowWarningMessage(string message)
        {
            IConsole console = _consoleProvider.CreateOutputConsole(requirePowerShellHost: false);
            console.WriteLine(message);
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
            // alternatively could get body of WizardData element from replacementsDictionary["$wizarddata$"] instead of parsing vstemplate file.
            RunStarted(automationObject, replacementsDictionary, runKind, customParams);
        }

        bool IWizard.ShouldAddProjectItem(string filePath)
        {
            // always add all project items
            return true;
        }

        internal static string DetermineSolutionDirectory(Dictionary<string, string> replacementsDictionary)
        {
            // the $solutiondirectory$ parameter is available in VS11 RC and later
            // No $solutiondirectory$? Ok, we're in the case where the solution is in 
            // the same directory as the project
            // Is $specifiedsolutionname$ null or empty? We're definitely in the solution
            // in same directory as project case.

            string solutionName;
            string solutionDir;
            bool ignoreSolutionDir = (replacementsDictionary.TryGetValue("$specifiedsolutionname$", out solutionName) && String.IsNullOrEmpty(solutionName));

            // We check $destinationdirectory$ twice because we want the following precedence:
            // 1. If $specifiedsolutionname$ == null, ALWAYS use $destinationdirectory$
            // 2. Otherwise, use $solutiondirectory$ if available
            // 3. If $solutiondirectory$ is not available, use $destinationdirectory$.
            if ((ignoreSolutionDir && replacementsDictionary.TryGetValue("$destinationdirectory$", out solutionDir)) ||
                replacementsDictionary.TryGetValue("$solutiondirectory$", out solutionDir) ||
                replacementsDictionary.TryGetValue("$destinationdirectory$", out solutionDir))
            {
                return solutionDir;
            }
            return null;
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

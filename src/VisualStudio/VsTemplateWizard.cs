using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.TemplateWizard;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio {
    [Export(typeof(IVsTemplateWizard))]
    public class VsTemplateWizard : IVsTemplateWizard {
        private readonly IVsPackageInstaller _installer;
        private InstallerConfiguration _configuration;
        private Project _project;

        private DTE DTE { get; set; }

        [ImportingConstructor]
        public VsTemplateWizard(IVsPackageInstaller installer) {
            _installer = installer;
        }

        private InstallerConfiguration GetConfigurationFromVsTemplateFile(string vsTemplatePath) {
            XDocument document = LoadDocument(vsTemplatePath);

            return GetConfigurationFromXmlDocument(document, vsTemplatePath);
        }

        internal InstallerConfiguration GetConfigurationFromXmlDocument(XDocument document, string vsTemplatePath, IVsExtensionManager vsExtensionManager = null) {
            IEnumerable<PackageInfo> packages = Enumerable.Empty<PackageInfo>();
            string repositoryPath = null;

            // Ignore XML namespaces since VS does not check them either when loading vstemplate files.
            XElement packagesElement = document.Root.ElementsNoNamespace("WizardData")
                .ElementsNoNamespace("packages")
                .FirstOrDefault();

            if (packagesElement != null) {
                RepositoryType repositoryType = GetRepositoryType(packagesElement);
                packages = GetPackages(packagesElement).ToList();

                if (packages.Any()) {
                    repositoryPath = GetRepositoryPath(packagesElement, repositoryType, vsTemplatePath,
                        vsExtensionManager);
                }
            }

            return new InstallerConfiguration(repositoryPath, packages);
        }

        private IEnumerable<PackageInfo> GetPackages(XElement packagesElement) {
            var declarations = (from packageElement in packagesElement.ElementsNoNamespace("package")
                                let id = packageElement.GetOptionalAttributeValue("id")
                                let version = packageElement.GetOptionalAttributeValue("version")
                                select new { id, version }).ToList();

            Version v;
            var missingOrInvalidAttributes = from declaration in declarations
                                             where
                                                 String.IsNullOrWhiteSpace(declaration.id) ||
                                                 String.IsNullOrWhiteSpace(declaration.version) ||
                                                 !Version.TryParse(declaration.version, out v)
                                             select declaration;

            if (missingOrInvalidAttributes.Any()) {
                ShowErrorMessage(
                    VsResources.TemplateWizard_InvalidPackageElementAttributes);
                throw new WizardBackoutException();
            }

            return from declaration in declarations
                   select new PackageInfo(declaration.id, declaration.version);
        }

        private string GetRepositoryPath(XElement packagesElement, RepositoryType repositoryType, string vsTemplatePath, IVsExtensionManager vsExtensionManager) {
            switch (repositoryType) {
                case RepositoryType.Template:
                    return Path.GetDirectoryName(vsTemplatePath);
                case RepositoryType.Extension:
                    string repositoryId = packagesElement.GetOptionalAttributeValue("repositoryId");
                    if (repositoryId == null) {
                        ShowErrorMessage(VsResources.TemplateWizard_MissingExtensionId);
                        throw new WizardBackoutException();
                    }
                    var extensionManager = vsExtensionManager ??
                                           ServiceLocator.GetGlobalService<SVsExtensionManager, IVsExtensionManager>();
                    IInstalledExtension extension;
                    if (!extensionManager.TryGetInstalledExtension(repositoryId, out extension)) {
                        ShowErrorMessage(String.Format(VsResources.TemplateWizard_InvalidExtensionId,
                            repositoryId));
                        throw new WizardBackoutException();
                    }
                    return Path.Combine(extension.InstallPath, "Packages");
            }
            // should not happen
            return null;
        }

        private RepositoryType GetRepositoryType(XElement packagesElement) {
            string repositoryAttributeValue = packagesElement.GetOptionalAttributeValue("repository");
            switch (repositoryAttributeValue) {
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

        internal virtual XDocument LoadDocument(string path) {
            return XDocument.Load(path);
        }

        private void PerformPackageInstall(IVsPackageInstaller packageInstaller, Project project, string packageRepositoryPath, IEnumerable<PackageInfo> packages) {
            var failedPackages = new List<PackageInfo>();

            foreach (var package in packages) {
                try {
                    DTE.StatusBar.Text = String.Format(CultureInfo.CurrentCulture, VsResources.TemplateWizard_PackageInstallStatus, package.Id, package.Version);
                    // TODO review parameters and installer call
                    // REVIEW is it OK to ignoreDependencies? The expectation is that the vstemplate will list all the required packagesr
                    packageInstaller.InstallPackage(packageRepositoryPath, project, package.Id, package.Version, ignoreDependencies: true);
                }
                catch (InvalidOperationException) {
                    failedPackages.Add(package);
                }
            }

            if (failedPackages.Any()) {
                var errorString = new StringBuilder();
                errorString.AppendFormat(VsResources.TemplateWizard_FailedToInstallPackage, packageRepositoryPath);
                errorString.AppendLine();
                errorString.AppendLine();
                errorString.Append(String.Join(Environment.NewLine, failedPackages.Select(p => p.Id + "." + p.Version)));
                ShowErrorMessage(errorString.ToString());
            }
        }

        private void ProjectFinishedGenerating(Project project) {
            _project = project;
        }

        private void RunFinished() {
            Debug.Assert(_project != null);
            Debug.Assert(_configuration != null);
            if (_configuration.Packages.Any()) {
                string repositoryPath = _configuration.RepositoryPath;
                PerformPackageInstall(_installer, _project, repositoryPath, _configuration.Packages);
            }
        }

        private void RunStarted(object automationObject, WizardRunKind runKind, object[] customParams) {
            if (runKind != WizardRunKind.AsNewProject) {
                ShowErrorMessage(VsResources.TemplateWizard_InvalidWizardRunKind);
                throw new WizardBackoutException();
            }

            DTE = (DTE)automationObject;
            var vsTemplatePath = (string)customParams[0];
            _configuration = GetConfigurationFromVsTemplateFile(vsTemplatePath);
        }

        internal virtual void ShowErrorMessage(string message) {
            MessageHelper.ShowErrorMessage(message, VsResources.TemplateWizard_ErrorDialogTitle);
        }

        void IWizard.BeforeOpeningFile(ProjectItem projectItem) {
            // do nothing
        }

        void IWizard.ProjectFinishedGenerating(Project project) {
            ProjectFinishedGenerating(project);
        }

        void IWizard.ProjectItemFinishedGenerating(ProjectItem projectItem) {
            // do nothing
        }

        void IWizard.RunFinished() {
            RunFinished();
        }

        void IWizard.RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams) {
            // TODO REVIEW alternatively could get body of WizardData element from replacementsDictionary["$wizarddata$"] instead of parsing vstemplate file.
            RunStarted(automationObject, runKind, customParams);
        }

        bool IWizard.ShouldAddProjectItem(string filePath) {
            // always add all project items
            return true;
        }

        internal sealed class InstallerConfiguration {
            public InstallerConfiguration(string repositoryPath, IEnumerable<PackageInfo> packages) {
                Packages = packages.ToList().AsReadOnly();
                RepositoryPath = repositoryPath;
            }

            public ICollection<PackageInfo> Packages { get; private set; }
            public string RepositoryPath { get; private set; }
        }

        internal sealed class PackageInfo {
            public PackageInfo(string id, string version) {
                Debug.Assert(!String.IsNullOrWhiteSpace(id));
                Debug.Assert(!String.IsNullOrWhiteSpace(version));

                Id = id;
                Version = new Version(version);
            }

            public string Id { get; private set; }
            public Version Version { get; private set; }
        }

        private enum RepositoryType {
            /// <summary>
            /// Cache location relative to the template (inside the same folder as the vstemplate file)
            /// </summary>
            Template,
            /// <summary>
            /// Cache location relative to the VSIX that packages the project template
            /// </summary>
            Extension,
        }
    }
}

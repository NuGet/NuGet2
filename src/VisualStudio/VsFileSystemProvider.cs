using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;

namespace NuGet.VisualStudio {
    [Export(typeof(IFileSystemProvider))]
    public class VsFileSystemProvider : IFileSystemProvider {
        private const string SolutionSection = "solution";
        private const string DisableSourceControlIntegerationKey = "disableSourceControlIntegration";
        private readonly DTE _dte;
        private readonly IComponentModel _componentModel;
        private readonly ISettings _settings;

        public VsFileSystemProvider()
            : this(ServiceLocator.GetInstance<DTE>(),
                   ServiceLocator.GetGlobalService<SComponentModel, IComponentModel>(),
                   ServiceLocator.GetInstance<ISettings>()) {
        }

        public VsFileSystemProvider(DTE dte, IComponentModel componentModel, ISettings settings) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }

            if (componentModel == null) {
                throw new ArgumentNullException("componentModel");
            }

            if (settings == null) {
                throw new ArgumentNullException("settings");
            }

            _componentModel = componentModel;
            _dte = dte;
            _settings = settings;
        }

        public IFileSystem GetFileSystem(string path) {
            // Get the source control providers
            var physicalFileSystem = new PhysicalFileSystem(path);
            if (!IsSourceControlDisabled(_settings)) {
                return physicalFileSystem;
            }

            var providers = _componentModel.GetExtensions<ISourceControlFileSystemProvider>();

            // Get the repository path
            IFileSystem fileSystem = null;

            var sourceControl = (SourceControl2)_dte.SourceControl;
            if (providers.Any() && sourceControl != null) {
                SourceControlBindings binding = null;
                try {
                    // Get the binding for this solution
                    binding = sourceControl.GetBindings(_dte.Solution.FullName);
                }
                catch (NotImplementedException) {
                    // Some source control providers don't bother to implement this.
                    // TFS might be the only one using it
                }

                if (binding != null) {
                    fileSystem = providers.Select(provider => GetFileSystemFromProvider(provider, path, binding))
                                          .Where(fs => fs != null)
                                          .FirstOrDefault();
                }
            }

            return fileSystem ?? physicalFileSystem;
        }

        private static bool IsSourceControlDisabled(ISettings _settings) {
            var value = _settings.GetValue(SolutionSection, DisableSourceControlIntegerationKey);
            bool disableSourceControlIntegration;
            return !String.IsNullOrEmpty(value) && Boolean.TryParse(value, out disableSourceControlIntegration) && disableSourceControlIntegration;
        }


        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We should never fail")]
        private static IFileSystem GetFileSystemFromProvider(ISourceControlFileSystemProvider provider, string path, SourceControlBindings binding) {
            try {
                return provider.GetFileSystem(path, binding);
            }
            catch {
                // Ignore exceptions that can happen when some binaries are missing. e.g. TfsSourceControlFileSystemProvider
                // would throw a jitting error if TFS is not installed
            }

            return null;
        }
    }
}

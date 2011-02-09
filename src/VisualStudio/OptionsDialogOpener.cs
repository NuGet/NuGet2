using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;

namespace NuGet.VisualStudio {

    [Export(typeof(IOptionsDialogOpener))]
    internal class OptionsDialogOpener : IOptionsDialogOpener {

        // GUID of our PackageSources page, defined in ToolsOptionsPage.cs
        const string packageSourcesPageGuid = "2819C3B6-FC75-4CD5-8C77-877903DE864C";

        // GUID of our RecentPackages page, defined in RecentPackagesPage.cs
        const string recentPackagesPageGuid = "0F052CF7-BF62-4743-B190-87FA4D49421E";

        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public OptionsDialogOpener([Import("PackageServiceProvider")]IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider");
            }

            _serviceProvider = serviceProvider;
        }

        public void OpenOptionsDialog(NuGetOptionsPage activePage) {
            string optionsPageGuid;
            switch (activePage) {
                case NuGetOptionsPage.PackageSources:
                    optionsPageGuid = packageSourcesPageGuid;
                    break;

                case NuGetOptionsPage.RecentPackages:
                    optionsPageGuid = recentPackagesPageGuid;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("activePage");
            }

            ShowOptionsPage(optionsPageGuid);
        }

        private void ShowOptionsPage(string pageGuid) {
            var command = new CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                VSConstants.cmdidToolsOptions);
            var mcs = (MenuCommandService)_serviceProvider.GetService(typeof(IMenuCommandService));
            mcs.GlobalInvoke(command, pageGuid);
        }
    }
}

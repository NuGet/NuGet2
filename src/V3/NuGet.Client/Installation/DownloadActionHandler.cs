using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    /// <summary>
    /// Handles the Download package Action by downloading the specified package into the shared repository
    /// (packages folder) for the solution.
    /// </summary>
    public class DownloadActionHandler : IActionHandler
    {
        public Task Execute(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            Uri downloadUri;
            try
            {
                downloadUri = action.Package.Value<Uri>(Properties.NupkgUrl);
            }
            catch (UriFormatException urifx)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_InvalidDownloadUrl,
                    action.PackageIdentity,
                    action.Package[Properties.NupkgUrl].ToString(),
                    urifx.Message));
            }
            if (downloadUri == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadActionHandler_NoDownloadUrl,
                    action.PackageIdentity));
            }

            return Task.Run(() =>
            {
                // Use the core-interop feature to execute the action
                var interop = target.GetRequiredFeature<NuGetCoreInstallationFeature>();
                var package = interop.DownloadPackage(
                    action.PackageIdentity,
                    downloadUri,
                    logger);

                // Run init.ps1 if present. Init is run WITHOUT specifying a target framework.
                ActionHandlerHelpers.ExecutePowerShellScriptIfPresent(
                    "init.ps1",
                    target,
                    action.Target,
                    package,
                    logger);
            });
        }

        public Task Rollback(NewPackageAction action, InstallationTarget target, IExecutionLogger logger)
        {
            // Just run the purge action to undo a download
            return new PurgeActionHandler().Execute(action, target, logger);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;
using NuGet.Client.Installation;

namespace NuGet.Client.Resolution
{
    /// <summary>
    /// Handles the Download package Action by downloading the specified package into the shared repository
    /// (packages folder) for the solution.
    /// </summary>
    public class DownloadActionHandler : IActionHandler
    {
        public Task Execute(PackageAction action, InstallationHost host, IExecutionLogger logger)
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

            // Use the core-interop feature to execute the action
            var interop = host.GetRequiredFeature<CoreInteropFeature>();
            return interop.DownloadPackage(
                action.PackageIdentity,
                downloadUri);
        }

        public Task Rollback(PackageAction action, InstallationHost host, IExecutionLogger logger)
        {
            // Just run the purge action to undo a download
            return new PurgeActionHandler().Execute(action, host, logger);
        }
    }
}

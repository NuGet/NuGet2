using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Microsoft.PowerShell.Commands;

namespace NuGet.Cmdlets
{
    public static class PSPathUtility
    {
        /// <summary>
        /// Translate a PSPath into a System.IO.* friendly Win32 path.
        /// Does not resolve/glob wildcards.
        /// </summary>        
        /// <param name="session">The SessionState to use.</param>
        /// <param name="psPath">The PowerShell PSPath to translate which may reference PSDrives or have provider-qualified paths which are syntactically invalid for .NET APIs.</param>
        /// <param name="path">The translated PSPath in a format understandable to .NET APIs.</param>
        /// <param name="exists">Returns null if not tested, or a bool representing path existence.</param>
        /// <param name="errorMessage">If translation failed, contains the reason.</param>
        /// <returns>True if successfully translated, false if not.</returns>
        public static bool TryTranslatePSPath(SessionState session, string psPath, out string path, out bool? exists, out string errorMessage) {
            if (String.IsNullOrEmpty(psPath)) {
                throw new ArgumentException("psPath");
            }

            bool succeeded = false;

            path = null;
            exists = null;
            errorMessage = null;

            if (!session.Path.IsValid(psPath)) {
                errorMessage = String.Format(Resources.Cmdlet_InvalidPathSyntax, psPath);
            }
            else {
                try {
                    // we do not glob wildcards (literalpath.)
                    exists = session.InvokeProvider.Item.Exists(psPath, force: false, literalPath: true);

                    ProviderInfo provider;
                    PSDriveInfo drive;

                    // translate pspath, not trying to glob.
                    path = session.Path.GetUnresolvedProviderPathFromPSPath(psPath, out provider, out drive);

                    // ensure path is on the filesystem (not registry, certificate, variable etc.)
                    if (provider.ImplementingType != typeof (FileSystemProvider)) {
                        errorMessage = Resources.Cmdlet_InvalidProvider;
                    }
                    else {
                        succeeded = true;
                    }
                }
                catch (ProviderNotFoundException) {
                    errorMessage = Resources.Cmdlet_InvalidProvider;
                }
                catch (DriveNotFoundException ex) {
                    errorMessage = String.Format(Resources.Cmdlet_InvalidPSDrive, ex.ItemName);
                }
            }
            return succeeded;
        }
    }
}

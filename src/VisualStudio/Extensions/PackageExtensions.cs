using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace NuGet.VisualStudio
{
    public static class PackageExtensions
    {
        public static bool HasPowerShellScript(this IPackage package)
        {
            return package.HasPowerShellScript(new string[] { PowerShellScripts.Init, PowerShellScripts.Install, PowerShellScripts.Uninstall });
        }

        public static bool HasPowerShellScript(this IPackage package, string[] names)
        {
            return package.GetFiles().Any(file => names.Any(name => file.Path.EndsWith(name, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool HasReadMeFileAtRoot(this IPackage package)
        {
            return package.GetFiles().Any(f => f.Path.Equals(NuGetConstants.ReadmeFileName, StringComparison.OrdinalIgnoreCase));
        }

        // the returned scriptPath is the relative path inside the package
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
        public static bool FindCompatibleToolFiles(
            this IPackage package,
            string scriptName,
            FrameworkName targetFramework,
            out IPackageFile toolFile)
        {
            if (scriptName.Equals("init.ps1", StringComparison.OrdinalIgnoreCase))
            {
                IPackageFile initFile = package.GetToolFiles()
                                               .FirstOrDefault(a => a.Path.Equals("tools\\init.ps1", StringComparison.OrdinalIgnoreCase));
                if (initFile != null)
                {
                    toolFile = initFile;
                    return true;
                }

                toolFile = null;
                return false;
            }

            // this is the case for either install.ps1 or uninstall.ps1
            // search for the correct script according to target framework of the project
            IEnumerable<IPackageFile> toolFiles;
            if (VersionUtility.TryGetCompatibleItems(targetFramework, package.GetToolFiles(), out toolFiles))
            {
                IPackageFile foundToolFile = toolFiles.FirstOrDefault(p => p.EffectivePath.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
                if (foundToolFile != null && !foundToolFile.IsEmptyFolder())
                {
                    toolFile = foundToolFile;
                    return true;
                }
            }

            toolFile = null;
            return false;
        }
    }
}
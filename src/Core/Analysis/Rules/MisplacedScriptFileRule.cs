using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NuGet.Resources;

namespace NuGet.Analysis.Rules
{
    internal class MisplacedScriptFileRule : IPackageRule
    {
        private const string ScriptExtension = ".ps1";

        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            foreach (IPackageFile file in package.GetFiles())
            {
                string path = file.Path;
                if (!path.EndsWith(ScriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!path.StartsWith(Constants.ToolsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    yield return CreatePackageIssueForMisplacedScript(path);
                }
                else
                {
                    string directory = Path.GetDirectoryName(path);
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (!directory.Equals(Constants.ToolsDirectory, StringComparison.OrdinalIgnoreCase) ||
                        !name.Equals("install", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("uninstall", StringComparison.OrdinalIgnoreCase) &&
                        !name.Equals("init", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForUnrecognizedScripts(path);
                    }
                }
            }
        }

        private static PackageIssue CreatePackageIssueForMisplacedScript(string target)
        {
            return new PackageIssue(
                AnalysisResources.ScriptOutsideToolsTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.ScriptOutsideToolsDescription, target),
                AnalysisResources.ScriptOutsideToolsSolution
            );
        }

        private static PackageIssue CreatePackageIssueForUnrecognizedScripts(string target)
        {
            return new PackageIssue(
                AnalysisResources.UnrecognizedScriptTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.UnrecognizedScriptDescription, target),
                AnalysisResources.UnrecognizedScriptSolution
            );
        }
    }
}
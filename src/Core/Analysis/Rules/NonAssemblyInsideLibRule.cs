using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet.Analysis.Rules {
    internal class NonAssemblyInsideLibRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package) {
            IEnumerable<string> allLibFiles = package.GetFiles(Constants.LibDirectory).Select(p => p.Path);
            var assembliesSet = new HashSet<string>(allLibFiles.Where(PackageUtility.IsAssembly), StringComparer.OrdinalIgnoreCase);

            return from path in allLibFiles
                   where !PackageUtility.IsAssembly(path) && !HasMatchingPdbOrXml(path, assembliesSet)
                   select CreatePackageIssue(path);
        }

        private static bool HasMatchingPdbOrXml(string path, HashSet<string> assemblies) {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) {

                return assemblies.Contains(Path.ChangeExtension(path, ".dll")) ||
                       assemblies.Contains(Path.ChangeExtension(path, ".exe"));
            }

            return false;
        }

        private static PackageIssue CreatePackageIssue(string target) {
            return new PackageIssue(
                AnalysisResources.NonAssemblyInLibTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.NonAssemblyInLibDescription, target),
                AnalysisResources.NonAssemblyInLibSolution
            );
        }
    }
}
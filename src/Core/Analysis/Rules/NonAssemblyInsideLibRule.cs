using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Analysis.Rules {
    internal class NonAssemblyInsideLibRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package) {
            IEnumerable<string> allLibFiles = package.GetFiles("lib").Select(p => p.Path);
            var assembliesSet = new HashSet<string>(allLibFiles.Where(IsAssembly), StringComparer.OrdinalIgnoreCase);

            return from path in allLibFiles
                   where !IsAssembly(path) && !IsMatchingPdbOrXml(path, assembliesSet)
                   select CreatePackageIssue(path);
        }

        private static bool IsMatchingPdbOrXml(string path, HashSet<string> assemblies) {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) {

                return assemblies.Contains(Path.ChangeExtension(path, ".dll")) ||
                       assemblies.Contains(Path.ChangeExtension(path, ".exe"));
            }

            return false;
        }

        private static bool IsAssembly(string path) {
            return path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        private static PackageIssue CreatePackageIssue(string target) {
            return new PackageIssue(
                "Incompatible files in lib folder",
                "The file '" + target + "' is not a valid assembly. If it is a XML documentation file or a .pdb file, there is no matching .dll file specified in the same folder.",
                "Either remove this file from 'lib' folder or add a matching .dll for it."
            );
        }
    }
}
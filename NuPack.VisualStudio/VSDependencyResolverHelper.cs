using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace NuPack.VisualStudio {
    internal static class VSDependencyResolverHelper {
        private const string ToolsDir = "tools";

        public static PackageType ResolvePackageTypes(IVsDependencyResolver resolver, 
                                                      IPackage package, 
                                                      Func<IPackage, IEnumerable<IPackage>> getDependencies, 
                                                      Func<IPackage, PackageOperation> getOperation) {

            // By default mark all packages as meta packages
            PackageType packageType = PackageType.Meta;

            if (package.HasProjectContent()) {
                // Any project content means we're trying to apply this package to a project
                packageType = PackageType.Project;
            }
            else if (!package.Dependencies.Any() && package.GetFiles(ToolsDir).Any()) {
                // If this package is only tools and has no dependencies then it's a solution package
                packageType = PackageType.Solution;
            }

            var dependencyTypes = new List<PackageType>();
            foreach (IPackage dependency in getDependencies(package)) {
                // Resolve the dependency type
                PackageType dependencyType = ResolvePackageTypes(resolver, dependency, getDependencies, getOperation);
                Debug.Assert(dependencyType != PackageType.Meta);

                // Solution packages can only have soltion dependencies
                if (packageType == PackageType.Solution && dependencyType == PackageType.Project) {
                    throw new InvalidOperationException("Solution only packages can only have solution only dependencies");
                }

                dependencyTypes.Add(dependencyType);
            }

            if (packageType == PackageType.Meta) {
                if (!dependencyTypes.Any()) {
                    throw new InvalidOperationException("Meta packages must have dependencies");
                }
                else if (dependencyTypes.All(type => type == PackageType.Solution)) {
                    packageType = PackageType.Solution;
                }
                else if (dependencyTypes.All(type => type == PackageType.Project)) {
                    packageType = PackageType.Project;
                }
                else {
                    throw new InvalidOperationException("Meta packages must be all solution level or all project level");
                }
            }

            // Get the operation for ths package
            PackageOperation operation = getOperation(package);

            if (packageType == PackageType.Project) {
                resolver.AddProjectOperation(operation);
            }
            else if (packageType == PackageType.Solution) {
                resolver.AddSolutionOperation(operation);
            }

            return packageType;
        }

        internal enum PackageType {
            Meta,
            Solution,
            Project,
        }
    }
}

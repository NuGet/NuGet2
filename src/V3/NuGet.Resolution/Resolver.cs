using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Resolution
{
    public class Resolver
    {
        private const string AbsentPropertyName = "___ABSENT";
        private PackageActionType operation;
        private PackageIdentity package;
        private Project project;
        private SourceRepository source;

        public bool IgnoreDependencies { get; set; }

        public DependencyBehavior DependencyVersion { get; set; }

        public bool AllowPrereleaseVersions { get; set; }

        public Resolver(PackageActionType operation, PackageIdentity packageIdentity, SourceRepository source)
        {
            this.operation = operation;
            this.package = packageIdentity;
            this.source = source;
        }

        public void AddOperationTarget(Project project)
        {
            //TODO: support multiple targets
            this.project = project;
        }

        private static bool IsAbsentPackage(JObject package)
        {
            return package.Property(AbsentPropertyName) != null;
        }

        public async Task<IEnumerable<NewPackageAction>> ResolveActionsAsync()
        {
            //TODO: Support multiple projects/operations. For now, only support a single project/operation/target.
            //TODO: Support proper merging/diffing to a target that already contains installed packages
            if (operation != PackageActionType.Install)
            {
                throw new NotSupportedException();
            }

            var packageMetadata = await source.GetPackageMetadata(package.Id, package.Version);

            IDictionary<string, ISet<JObject>> dependenciesById = new Dictionary<string, ISet<JObject>>(StringComparer.OrdinalIgnoreCase);
            if (!IgnoreDependencies)
            {
                IEnumerable<JObject> dependencyCandidates = GetDependencyCandidates(packageMetadata.GetDependencies(), new Stack<JObject>(new[] { packageMetadata }));

                foreach (var group in dependencyCandidates.GroupBy(dc => dc.GetId()))
                {
                    var set = new HashSet<JObject>(group, new PackageEqualityComparer());
                    //Create an 'absent' package for each dependency package Id.
                    set.Add(new JObject(new JProperty(Properties.PackageId, group.Key), new JProperty(AbsentPropertyName, true)));
                    dependenciesById.Add(group.Key, set);
                }
            }

            var candidates = new List<IEnumerable<JObject>> { new List<JObject> { packageMetadata } }.Concat(dependenciesById.Values);

            var comparer = new CompareWrapper<JObject>((x, y) =>
            {
                System.Diagnostics.Debug.Assert(string.Equals(x.GetId(), y.GetId(), StringComparison.InvariantCultureIgnoreCase));

                // The absent package comes first in the sort order
                bool isXAbsent = IsAbsentPackage(x);
                bool isYAbsent = IsAbsentPackage(y);
                if (isXAbsent && !isYAbsent)
                {
                    return -1;
                }
                if (!isXAbsent && isYAbsent)
                {
                    return 1;
                }
                if (isXAbsent && isYAbsent)
                {
                    return 0;
                }

                if (this.project != null)
                {
                    //Already installed packages come next in the sort order.
                    bool xInstalled = this.project.InstalledPackages.IsInstalled(x.GetId(), x.GetVersion());
                    bool yInstalled = this.project.InstalledPackages.IsInstalled(y.GetId(), y.GetVersion());
                    if (xInstalled && !yInstalled)
                    {
                        return -1;
                    }

                    if (!xInstalled && yInstalled)
                    {
                        return 1;
                    }
                }

                var xv = x.GetVersion();
                var yv = y.GetVersion();

                switch (DependencyVersion)
                {
                    case DependencyBehavior.Lowest:
                        return VersionComparer.Default.Compare(xv, yv);
                    case DependencyBehavior.Highest:
                        return -1 * VersionComparer.Default.Compare(xv, yv);
                    case DependencyBehavior.HighestMinor:
                        {
                            if (VersionComparer.Default.Equals(xv, yv)) return 0;

                            //TODO: This is surely wrong...
                            return new[] { x, y }.OrderBy(p => p.GetVersion().Major)
                                               .ThenByDescending(p => p.GetVersion().Minor)
                                               .ThenByDescending(p => p.GetVersion().Patch).FirstOrDefault() == x ? -1 : 1;

                        }
                    case DependencyBehavior.HighestPatch:
                        {
                            if (VersionComparer.Default.Equals(xv, yv)) return 0;

                            //TODO: This is surely wrong...
                            return new[] { x, y }.OrderBy(p => p.GetVersion().Major)
                                                 .ThenBy(p => p.GetVersion().Minor)
                                                 .ThenByDescending(p => p.GetVersion().Patch).FirstOrDefault() == x ? -1 : 1;
                        }
                    default:
                        throw new InvalidOperationException("Unknown DependencyBehavior value.");
                }
            });

            var shouldRejectPackagePair = new Func<JObject, JObject, bool>((p1, p2) =>
            {
                var p1ToP2Dependency = p1.FindDependencyRange(p2.GetId());
                if (p1ToP2Dependency != null)
                {
                    return IsAbsentPackage(p2) || !p1ToP2Dependency.Satisfies(p2.GetVersion());
                }

                var p2ToP1Dependency = p2.FindDependencyRange(p1.GetId());
                if (p2ToP1Dependency != null)
                {
                    return IsAbsentPackage(p1) || !p2ToP1Dependency.Satisfies(p1.GetVersion());
                }

                return false;
            });

            var solver = new CombinationSolver<JObject>();
            var solution = solver.FindSolution(candidates, comparer, shouldRejectPackagePair);

            if (solution == null)
            {
                throw new InvalidOperationException("Unable to determine set of packages to install.");
            }

            var nonAbsentCandidates = solution.Where(c => !IsAbsentPackage(c));

            var sortedSolution = TopologicalSort(nonAbsentCandidates);

            var uninstallActionsForUpgrade = GetUninstallActions(sortedSolution, this.project).Reverse();
            var installActions = GetInstallActions(sortedSolution, this.project);

            return uninstallActionsForUpgrade.Concat(installActions);
        }

        private IEnumerable<NewPackageAction> GetUninstallActions(IEnumerable<JObject> solution, Project project)
        {
            if (this.project != null)
            {
                var installedPackages = this.project.InstalledPackages.GetAllInstalledPackagesAndMetadata().GetAwaiter().GetResult();

                return solution.SelectMany(node => installedPackages.Where(p => p.GetId() == node.GetId() && p.GetVersion().CompareTo(node.GetVersion()) != 0)
                                                                    .Select(p => new NewPackageAction(PackageActionType.Uninstall, p.AsPackageIdentity(), p, null, source, null)));
            }

            return Enumerable.Empty<NewPackageAction>();
        }

        private IEnumerable<NewPackageAction> GetInstallActions(IEnumerable<JObject> solution, Project project)
        {
            return solution.Where(node => this.project == null || !this.project.InstalledPackages.IsInstalled(node.GetId(), node.GetVersion()))
                           .Select(node => new NewPackageAction(PackageActionType.Install, node.AsPackageIdentity(), node, null, source, null));
        }

        private IEnumerable<JObject> TopologicalSort(IEnumerable<JObject> nodes)
        {
            List<JObject> result = new List<JObject>();

            var dependsOn = new Func<JObject, JObject, bool>((x, y) =>
            {
                return x.FindDependencyRange(y.GetId()) != null;
            });

            var dependenciesAreSatisfied = new Func<JObject, bool>(node =>
            {
                var dependencies = node.GetDependencies();
                return dependencies == null || dependencies.Count == 0 ||
                       dependencies.All(d => result.Any(r => r.GetId() == d.Value<string>(Properties.PackageId)));
            });

            var satisfiedNodes = new HashSet<JObject>(nodes.Where(n => dependenciesAreSatisfied(n)));
            while (!satisfiedNodes.IsEmpty())
            {
                //Pick any element from the set. Remove it, and add it to the result list.
                var node = satisfiedNodes.First();
                satisfiedNodes.Remove(node);
                result.Add(node);

                // Find unprocessed nodes that depended on the node we just added to the result.
                // If all of its dependencies are now satisfied, add it to the set of nodes to process.
                var newlySatisfiedNodes = nodes.Except(result)
                                               .Where(n => dependsOn(n, node))
                                               .Where(n => dependenciesAreSatisfied(n));
                satisfiedNodes.AddRange(newlySatisfiedNodes);
            }

            return result;
        }

        private IEnumerable<JObject> GetDependencyCandidates(JArray dependencies, Stack<JObject> parents)
        {
            //TODO: This is naive/slow for now...no caching, etc....
            foreach (JObject dependency in dependencies)
            {
                if (parents.Any(p => p.GetId() == dependency.GetId()))
                {
                    var exceptionMessage = new StringBuilder("Circular dependency detected '");
                    //A 1.0 => B 1.0 => A 1.5
                    foreach (var parent in parents.Reverse())
                    {
                        exceptionMessage.AppendFormat("{0} {1} => ", parent.GetId(), parent.GetVersion());
                    }

                    exceptionMessage.Append(dependency.GetId());
                    var range = dependency.Value<string>(Properties.Range);
                    if (!string.IsNullOrEmpty(range))
                    {
                        exceptionMessage.AppendFormat(" {0}", range);
                    }
                    exceptionMessage.Append("'.");

                    throw new InvalidOperationException(exceptionMessage.ToString());
                }

                foreach (var candidate in ResolveDependencyCandidates(dependency))
                {
                    yield return candidate;

                    parents.Push(candidate);

                    foreach (var subCandidate in GetDependencyCandidates(candidate.GetDependencies(), parents))
                    {
                        yield return subCandidate;
                    }

                    parents.Pop();
                }
            }
        }

        private IEnumerable<JObject> ResolveDependencyCandidates(JObject dependency)
        {
            //TODO: yield installed packages first.
            //TODO: don't use GetAwaiter here. See if there is a way to make this async.
            var packages = source.GetPackageMetadataById(dependency.Value<string>(Properties.PackageId)).GetAwaiter().GetResult();

            return packages.Where(p =>
            {
                var range = p.Value<string>(Properties.Range);
                if (string.IsNullOrEmpty(range))
                {
                    return true;
                }

                IVersionSpec rangeSpec;
                SemanticVersion version;
                if (VersionUtility.TryParseVersionSpec(range, out rangeSpec) &&
                   SemanticVersion.TryParse(p.Value<string>(Properties.Version), out version))
                {
                    return rangeSpec.Satisfies(version);
                }

                return false;
            });
        }

        private class PackageEqualityComparer : IEqualityComparer<JObject>
        {
            public bool Equals(JObject x, JObject y)
            {
                return x.GetId() == y.GetId() && VersionComparer.Default.Equals(x.GetVersion(), y.GetVersion());
            }

            public int GetHashCode(JObject obj)
            {
                return (obj.GetId() + obj.GetVersionAsString()).GetHashCode();
            }
        }

        /// <summary>
        /// Simple helper class to provide an IComparer instance based on a comparison function
        /// </summary>
        /// <typeparam name="T">The type to compare.</typeparam>
        private class CompareWrapper<T> : IComparer<T>
        {
            private readonly Func<T, T, int> compareImpl;

            public CompareWrapper(Func<T, T, int> compareImpl)
            {
                if (compareImpl == null)
                {
                    throw new ArgumentNullException("compareImpl");
                }
                this.compareImpl = compareImpl;
            }

            public int Compare(T x, T y)
            {
                return compareImpl(x, y);
            }
        }
    }
}

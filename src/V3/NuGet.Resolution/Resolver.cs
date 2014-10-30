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
        private PackageActionType operation;
        private PackageIdentity package;
        private Project project;
        private SourceRepository source;

        public bool IgnoreDependencies { get; set; }

        public DependencyBehavior DependencyVersion { get; set; }

        public bool AllowPrereleaseVersions { get; set; }

        public void AddOperation(PackageActionType operation, PackageIdentity packageIdentity, Project project, SourceRepository source)
        {
            this.operation = operation;
            this.package = packageIdentity;
            this.project = project;
            this.source = source;
        }

        public async Task<IEnumerable<NewPackageAction>> ResolveActionsAsync()
        {
            //TODO: Support multiple projects/operations. For now, only support a single project/operation/target.
            //TODO: Support proper merging/diffing to a target that already contains installed packages
            var packageMetadata = await source.GetPackageMetadata(package.Id, package.Version);

            IDictionary<string, ISet<JObject>> dependenciesById = new Dictionary<string, ISet<JObject>>(StringComparer.OrdinalIgnoreCase);
            if (!IgnoreDependencies)
            {
                IEnumerable<JObject> dependencyCandidates = GetDependencyCandidates(packageMetadata.GetDependencies());

                foreach (var group in dependencyCandidates.GroupBy(dc => dc.GetId()))
                {
                    dependenciesById.Add(group.Key, new HashSet<JObject>(group, new PackageEqualityComparer()));
                }
            }

            var candidates = new List<IEnumerable<JObject>> { new List<JObject> { packageMetadata } }.Concat(dependenciesById.Values);
            var solver = new CombinationSolver<JObject>();

            var comparer = new CompareWrapper<JObject>((x, y) =>
            {
                System.Diagnostics.Debug.Assert(string.Equals(x.GetId(), y.GetId(), StringComparison.InvariantCultureIgnoreCase));
                
                //TODO: Uncomment this!!!
                ////Already installed packages come next in the sort order.
                //var installedPackageX = Repository.FindPackage(x.Id, x.Version);
                //var installedPackageY = Repository.FindPackage(y.Id, y.Version);

                //if (installedPackageX != null && installedPackageY == null)
                //{
                //    return -1;
                //}
                //if (installedPackageY != null && installedPackageX == null)
                //{
                //    return 1;
                //}

                var xv = x.GetVersion();
                var yv = y.GetVersion();

                switch(DependencyVersion)
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
                    return !p1ToP2Dependency.Satisfies(p2.GetVersion());
                }

                var p2ToP1Dependency = p2.FindDependencyRange(p1.GetId());
                if (p2ToP1Dependency != null)
                {
                    return !p2ToP1Dependency.Satisfies(p1.GetVersion());
                }

                return false;
            });

            var solution = solver.FindSolution(candidates, comparer, shouldRejectPackagePair);

            if (solution == null)
            {
                throw new InvalidOperationException("Unable to determine set of packages to install.");
            }

            //TODO: Do a diff from the current dependency graph...determine operations to perform to get to solution.
            return solution.Select(c => new NewPackageAction(PackageActionType.Install, c.AsPackageIdentity(), c, null, source, null));
        }

        private IEnumerable<JObject> GetDependencyCandidates(JArray dependencies)
        {
            //TODO: This is incredibly naive/slow for now...no caching, etc....
            foreach (JObject dependency in dependencies)
            {
                foreach (var candidate in ResolveDependencyCandidates(dependency))
                {
                    yield return candidate;

                    foreach (var subCandidate in GetDependencyCandidates(candidate.GetDependencies()))
                    {
                        yield return subCandidate;
                    }
                }
            }
        }

        private IEnumerable<JObject> ResolveDependencyCandidates(JObject dependency)
        {
            //TODO: yield installed packages first.
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
                if(VersionUtility.TryParseVersionSpec(range, out rangeSpec) &&
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
{
    public abstract class PackageWalker
    {
        private readonly Dictionary<IPackage, PackageWalkInfo> _packageLookup = new Dictionary<IPackage, PackageWalkInfo>();
        private readonly FrameworkName _targetFramework;

        protected PackageWalker()
            : this(targetFramework: null)
        {
        }

        protected PackageWalker(FrameworkName targetFramework)
        {
            _targetFramework = targetFramework;
            Marker = new PackageMarker();
            DependencyVersion = DependencyVersion.Lowest;
        }

        public virtual bool SkipPackageTargetCheck { get; set; }

        protected FrameworkName TargetFramework
        {
            get
            {
                return _targetFramework;
            }
        }

        protected virtual bool RaiseErrorOnCycle
        {
            get
            {
                return true;
            }
        }

        protected virtual bool SkipDependencyResolveError
        {
            get
            {
                return false;
            }
        }

        protected virtual bool IgnoreDependencies
        {
            get
            {
                return false;
            }
        }

        protected virtual bool AllowPrereleaseVersions
        {
            get
            {
                return true;
            }
        }

        public DependencyVersion DependencyVersion
        {
            get;
            set;
        }

        protected PackageMarker Marker
        {
            get;
            private set;
        }

        protected virtual bool IgnoreWalkInfo
        {
            get
            {
                return false;
            }
        }

        public void Walk(IPackage package)
        {
            CheckPackageMinClientVersion(package);

            // Do nothing if we saw this package already
            if (Marker.IsVisited(package))
            {
                ProcessPackageTarget(package);
                return;
            }

            OnBeforePackageWalk(package);

            // Mark the package as processing
            Marker.MarkProcessing(package);

            if (!IgnoreDependencies)
            {
                foreach (var dependency in package.GetCompatiblePackageDependencies(TargetFramework))
                {
                    // Try to resolve the dependency from the visited packages first
                    IPackage resolvedDependency = Marker.ResolveDependency(
                        dependency, constraintProvider: null, 
                        allowPrereleaseVersions: AllowPrereleaseVersions, 
                        preferListedPackages: false,
                        dependencyVersion: DependencyVersion) ??
                        ResolveDependency(dependency);

                    if (resolvedDependency == null)
                    {
                        OnDependencyResolveError(dependency);
                        // If we're skipping dependency resolve errors then move on to the next
                        // dependency
                        if (SkipDependencyResolveError)
                        {
                            continue;
                        }

                        return;
                    }

                    if (!IgnoreWalkInfo)
                    {
                        // Set the parent
                        PackageWalkInfo dependencyInfo = GetPackageInfo(resolvedDependency);
                        dependencyInfo.Parent = package;
                    }

                    Marker.AddDependent(package, resolvedDependency);

                    if (!OnAfterResolveDependency(package, resolvedDependency))
                    {
                        continue;
                    }

                    if (Marker.IsCycle(resolvedDependency) ||
                        Marker.IsVersionCycle(resolvedDependency.Id))
                    {
                        if (RaiseErrorOnCycle)
                        {
                            List<IPackage> packages = Marker.Packages.ToList();
                            packages.Add(resolvedDependency);

                            throw new InvalidOperationException(
                               String.Format(CultureInfo.CurrentCulture,
                               NuGetResources.CircularDependencyDetected, String.Join(" => ",
                               packages.Select(p => p.GetFullName()))));
                        }

                        continue;
                    }

                    Walk(resolvedDependency);
                }
            }

            // Mark the package as visited
            Marker.MarkVisited(package);

            ProcessPackageTarget(package);

            OnAfterPackageWalk(package);
        }

        private static void CheckPackageMinClientVersion(IPackage package)
        {
            // validate that the current version of NuGet satisfies the minVersion attribute specified in the .nuspec
            if (Constants.NuGetVersion < package.MinClientVersion)
            {
                throw new NuGetVersionNotSatisfiedException(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageMinVersionNotSatisfied, package.GetFullName(), package.MinClientVersion, Constants.NuGetVersion));
            }
        }

        /// <summary>
        /// Resolve the package target (i.e. if the parent package was a meta package then set the parent to the current project type)
        /// </summary>
        private void ProcessPackageTarget(IPackage package)
        {
            if (IgnoreWalkInfo || SkipPackageTargetCheck)
            {
                return;
            }

            PackageWalkInfo info = GetPackageInfo(package);

            // If our parent is an unknown then we need to bubble up the type
            if (info.Parent != null)
            {
                PackageWalkInfo parentInfo = GetPackageInfo(info.Parent);

                Debug.Assert(parentInfo != null);

                if (parentInfo.InitialTarget == PackageTargets.None)
                {
                    // Update the parent target type
                    parentInfo.Target |= info.Target;

                    // If we ended up with both that means we found a dependency only packages
                    // that has a mix of solution and project level packages
                    if (parentInfo.Target == PackageTargets.All)
                    {
                        throw new InvalidOperationException(NuGetResources.DependencyOnlyCannotMixDependencies);
                    }
                }

                // Solution packages can't depend on project level packages
                if (parentInfo.Target == PackageTargets.External && info.Target.HasFlag(PackageTargets.Project))
                {
                    throw new InvalidOperationException(NuGetResources.ExternalPackagesCannotDependOnProjectLevelPackages);
                }
            }
        }

        protected virtual bool OnAfterResolveDependency(IPackage package, IPackage dependency)
        {
            return true;
        }

        protected virtual void OnBeforePackageWalk(IPackage package)
        {
        }

        protected virtual void OnAfterPackageWalk(IPackage package)
        {
        }

        protected virtual void OnDependencyResolveError(PackageDependency dependency)
        {
        }

        protected abstract IPackage ResolveDependency(PackageDependency dependency);

        protected internal PackageWalkInfo GetPackageInfo(IPackage package)
        {
            PackageWalkInfo info;
            if (!_packageLookup.TryGetValue(package, out info))
            {
                info = new PackageWalkInfo(GetPackageTarget(package));
                _packageLookup.Add(package, info);
            }
            return info;
        }

        private static PackageTargets GetPackageTarget(IPackage package)
        {
            if (package.HasProjectContent())
            {
                return PackageTargets.Project;
            }

            if (IsDependencyOnly(package))
            {
                return PackageTargets.None;
            }

            return PackageTargets.External;
        }

        /// <summary>
        /// Returns true if a package has dependencies but no \tools directory
        /// </summary>
        private static bool IsDependencyOnly(IPackage package)
        {
            return !package.GetFiles().Any(f => f.Path.StartsWith(@"tools\", StringComparison.OrdinalIgnoreCase)) &&
                   package.DependencySets.SelectMany(d => d.Dependencies).Any();
        }
    }
}
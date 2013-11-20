using System;

namespace NuGet
{
    public interface IProjectManager
    {
        IPackageRepository LocalRepository { get; }
        ILogger Logger { get; set; }
        IProjectSystem Project { get; }
        IPackageRepository SourceRepository { get; }
        DependencyVersion DependencyVersion { get; set; }
        bool WhatIf { get; set; }

        event EventHandler<PackageOperationEventArgs> PackageReferenceAdded;
        event EventHandler<PackageOperationEventArgs> PackageReferenceAdding;
        event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved;
        event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving;

        void AddPackageReference(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions);
        void AddPackageReference(string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions);

        void RemovePackageReference(string packageId, bool forceRemove, bool removeDependencies);
        void RemovePackageReference(IPackage package, bool forceRemove, bool removeDependencies);

        void UpdatePackageReference(string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions);
        void UpdatePackageReference(string packageId, IVersionSpec versionSpec, bool updateDependencies, bool allowPrereleaseVersions);
        void UpdatePackageReference(IPackage remotePackage, bool updateDependencies, bool allowPrereleaseVersions);

        bool IsInstalled(IPackage package);
    }
}
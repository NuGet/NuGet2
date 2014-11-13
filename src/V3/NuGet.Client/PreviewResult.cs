using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NuGet.Client
{
    public class PreviewResult
    {
        public IEnumerable<PackageIdentity> Deleted
        {
            get;
            private set;
        }

        public IEnumerable<PackageIdentity> Added
        {
            get;
            private set;
        }

        public IEnumerable<PackageIdentity> Unchanged
        {
            get;
            private set;
        }

        public IEnumerable<UpdatePreviewResult> Updated
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        private PreviewResult(
            string name,
            IEnumerable<PackageIdentity> added,
            IEnumerable<PackageIdentity> deleted,
            IEnumerable<PackageIdentity> unchanged,
            IEnumerable<UpdatePreviewResult> updated)
        {
            Name = name;
            Added = added;
            Deleted = deleted;
            Unchanged = unchanged;
            Updated = updated;
        }

        public static IEnumerable<PreviewResult> CreatePreview(
            IEnumerable<NuGet.Client.Resolution.PackageAction> actions,
            Installation.InstallationTarget target)
        {
            var previewResults = new List<PreviewResult>();

            foreach (var targetProject in target.GetAllTargetsRecursively())
            {
                var result = CalculatePreviewForProject(actions, targetProject);

                if (result != null)
                {
                    previewResults.Add(result);
                }
            }

            return previewResults;
        }

        // Calulates the prevew result for the target project and returns the result.
        private static PreviewResult CalculatePreviewForProject(
            IEnumerable<NuGet.Client.Resolution.PackageAction> actions,
            Installation.InstallationTarget targetProject)
        {
            var existingPackages = targetProject.InstalledPackages.GetInstalledPackages()
                .Select(p => p.Identity)
                .ToList();

            var installed = new Dictionary<string, PackageIdentity>(StringComparer.OrdinalIgnoreCase);
            var uninstalled = new Dictionary<string, PackageIdentity>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in actions.Where(a => targetProject.Equals(a.Target)))
            {
                if (action.ActionType == NuGet.Client.Resolution.PackageActionType.Install)
                {
                    installed[action.PackageIdentity.Id] = action.PackageIdentity;
                }
                else if (action.ActionType == NuGet.Client.Resolution.PackageActionType.Uninstall)
                {
                    uninstalled[action.PackageIdentity.Id] = action.PackageIdentity;
                }
            }

            var addedPackages = new List<PackageIdentity>();
            var deletedPackages = new List<PackageIdentity>();
            var unchangedPackages = new List<PackageIdentity>();
            var updatedPackges = new List<UpdatePreviewResult>();

            // process existing packages to get updatedPackages, deletedPackages
            // and unchangedPackages
            foreach (var package in existingPackages)
            {
                var isInstalled = installed.ContainsKey(package.Id);
                var isUninstalled = uninstalled.ContainsKey(package.Id);

                if (isInstalled && isUninstalled)
                {
                    // the package is updated
                    updatedPackges.Add(new UpdatePreviewResult(package, installed[package.Id]));
                    installed.Remove(package.Id);
                }
                else if (isInstalled && !isUninstalled)
                {
                    // this can't happen
                    Debug.Assert(false, "We should never reach here");
                }
                else if (!isInstalled && isUninstalled)
                {
                    // the package is uninstalled
                    deletedPackages.Add(package);
                }
                else if (!isInstalled && !isUninstalled)
                {
                    // the package is unchanged
                    unchangedPackages.Add(package);
                }
            }

            // now calculate addedPackages
            foreach (var package in installed.Values)
            {
                if (!existingPackages.Contains(package))
                {
                    addedPackages.Add(package);
                }
            }

            if (addedPackages.Any() ||
                deletedPackages.Any() ||
                updatedPackges.Any())
            {
                return new PreviewResult(
                    targetProject.Name,
                    added: addedPackages,
                    deleted: deletedPackages,
                    unchanged: unchangedPackages,
                    updated: updatedPackges);
            }
            else
            {
                return null;
            }
        }
    }
}

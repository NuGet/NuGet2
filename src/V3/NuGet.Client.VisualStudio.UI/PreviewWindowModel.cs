using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuGet.Client.Resolution;

namespace NuGet.Client.VisualStudio.UI
{
    public class PreviewWindowModel
    {
        private List<PreviewResult> _previewResults;

        public IEnumerable<PreviewResult> PreviewResults
        {
            get
            {
                return _previewResults;
            }
        }

        public PreviewWindowModel(IEnumerable<PackageAction> actions, Installation.InstallationTarget target)
        {
            _previewResults = new List<PreviewResult>();

            foreach (var targetProject in target.GetAllTargetsRecursively())
            {
                CalculatePreviewForProject(actions, targetProject);
            }
        }


        // Calulates the prevew result for the target project and adds the result to _previewResults.
        private void CalculatePreviewForProject(
            IEnumerable<PackageAction> actions,
            Installation.InstallationTarget targetProject)
        {
            var existingPackages = targetProject.InstalledPackages.GetInstalledPackages()
                .Select(p => p.Identity)
                .ToList();

            var installed = new Dictionary<string, PackageIdentity>(StringComparer.OrdinalIgnoreCase);
            var uninstalled = new Dictionary<string, PackageIdentity>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in actions.Where(a => targetProject.Equals(a.Target)))
            {
                if (action.ActionType == PackageActionType.Install)
                {
                    installed[action.PackageIdentity.Id] = action.PackageIdentity;
                }
                else if (action.ActionType == PackageActionType.Uninstall)
                {
                    uninstalled[action.PackageIdentity.Id] = action.PackageIdentity;
                }
            }

            var addedPackages = new List<PackageIdentity>();
            var deletedPackages = new List<PackageIdentity>();
            var unchangedPackages = new List<PackageIdentity>();
            var updatedPackges = new List<UpdateResult>();

            // process existing packages to get updatedPackages, deletedPackages
            // and unchangedPackages
            foreach (var package in existingPackages)
            {
                var isInstalled = installed.ContainsKey(package.Id);
                var isUninstalled = uninstalled.ContainsKey(package.Id);

                if (isInstalled && isUninstalled)
                {
                    // the package is updated
                    updatedPackges.Add(new UpdateResult(package, installed[package.Id]));
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
                _previewResults.Add(new PreviewResult(
                    targetProject.Name,
                    added: addedPackages,
                    deleted: deletedPackages,
                    unchanged: unchangedPackages,
                    updated: updatedPackges));
            }
        }
    }

    public class UpdateResult
    {
        public PackageIdentity Old { get; private set; }
        public PackageIdentity New { get; private set; }

        public UpdateResult(PackageIdentity oldPackage, PackageIdentity newPackage)
        {
            Old = oldPackage;
            New = newPackage;
        }

        public override string ToString()
        {
            return Old.ToString() + " -> " + New.ToString();
        }
    }

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

        public IEnumerable<UpdateResult> Updated
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public PreviewResult(
            string name,
            IEnumerable<PackageIdentity> added,
            IEnumerable<PackageIdentity> deleted,
            IEnumerable<PackageIdentity> unchanged,
            IEnumerable<UpdateResult> updated)
        {
            Name = name;
            Added = added;
            Deleted = deleted;
            Unchanged = unchanged;
            Updated = updated;
        }
    }
}
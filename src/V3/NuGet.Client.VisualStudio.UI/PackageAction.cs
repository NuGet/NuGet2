using NuGet.PackagingCore;
using NuGet.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.UI
{
    public class PackageAction
    {
        public PackageIdentity PackageIdentity { get; private set; }
        public NuGetProject Target { get; private set; }
        public PackageActionType ActionType { get; private set; }
        public SourceRepository Source { get; private set; }
        public UIPackageMetadata Package { get; private set; }
        public PackageIdentity DependentPackage { get; private set; }
        public bool IsUpdate { get; set; }



        public PackageAction(PackageActionType actionType, PackageIdentity packageName, UIPackageMetadata package, NuGetProject target, SourceRepository source, PackageIdentity dependentPackage)
        {
            ActionType = actionType;
            PackageIdentity = packageName;
            Target = target;
            Source = source;
            DependentPackage = dependentPackage;
            IsUpdate = false;
            Package = package;
        }

        public override string ToString()
        {
            return
                ActionType.ToString() + " " + PackageIdentity.ToString() +
                (Target == null ? String.Empty : (" " + "TODO IMPL PROJECT NAME"));
        }

    }

    public enum PackageActionType
    {
        // installs a package into a project/solution
        Install,

        // uninstalls a package from a project/solution
        Uninstall,

        // downloads the package if needed and adds it to the packages folder
        AddToPackagesFolder,

        // deletes the package from the packages folder
        DeleteFromPackagesFolder
    }

}

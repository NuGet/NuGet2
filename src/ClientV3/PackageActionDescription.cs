using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public class PackageActionDescription
    {
        public PackageName Package { get; private set; }
        public string Target { get; private set; }
        public PackageActionType ActionType { get; private set; }

        public PackageActionDescription(PackageActionType actionType, PackageName package, string target)
        {
            ActionType = actionType;
            Package = package;
            Target = target;
        }
    }

    public enum PackageActionType
    {
        Install,
        Uninstall,
        Purge,
        Download,
        AcceptLicense
    }
}

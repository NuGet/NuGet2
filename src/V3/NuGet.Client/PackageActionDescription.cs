using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client
{
    public class PackageActionDescription
    {
        public PackageIdentity PackageName { get; private set; }
        public JObject Package { get; private set; }
        public string Target { get; private set; }
        public PackageActionType ActionType { get; private set; }

        public PackageActionDescription(PackageActionType actionType, PackageIdentity packageName, JObject package, string target)
        {
            ActionType = actionType;
            PackageName = packageName;
            Package = package;
            Target = target;
        }
    }

    public enum PackageActionType
    {
        Install,
        Uninstall,
        Purge,
        Download
    }
}

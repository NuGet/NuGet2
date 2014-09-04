using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NuGet.Versioning;

namespace NuGet.Client.Interop
{
    internal class PackageActionDescriptionWrapper : PackageActionDescription
    {
        internal NuGet.Resolver.PackageAction ResolverAction { get; private set; }

        public PackageActionDescriptionWrapper(NuGet.Resolver.PackageAction resolverAction)
            : base(
                ConvertAction(resolverAction.ActionType),
                new PackageIdentity(
                    resolverAction.Package.Id, 
                    new NuGetVersion(resolverAction.Package.Version.Version, resolverAction.Package.Version.SpecialVersion)),
                PackageJsonLd.CreatePackage(resolverAction.Package),
                GetTarget(resolverAction))
        {
            ResolverAction = resolverAction;
        }

        private static PackageActionType ConvertAction(Resolver.PackageActionType packageActionType)
        {
            switch (packageActionType)
            {
            case NuGet.Resolver.PackageActionType.Install:
                return PackageActionType.Install;
            case NuGet.Resolver.PackageActionType.Uninstall:
                return PackageActionType.Uninstall;
            case NuGet.Resolver.PackageActionType.AddToPackagesFolder:
                return PackageActionType.Download;
            case NuGet.Resolver.PackageActionType.DeleteFromPackagesFolder:
                return PackageActionType.Purge;
            default:
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, Strings.PackageActionDescriptionWrapper_UnrecognizedAction, packageActionType.ToString()),
                    "packageActionType");
            }
        }

        private static string GetTarget(Resolver.PackageAction resolverAction)
        {
            NuGet.Resolver.PackageProjectAction projectAction = resolverAction as NuGet.Resolver.PackageProjectAction;
            if (projectAction != null)
            {
                return projectAction.ProjectManager.Project.ProjectName;
            }
            return null;
        }
    }
}

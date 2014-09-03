using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public static class Types
    {
        public static readonly Uri PackageSearchResult = new Uri("http://schema.nuget.org/schema#PackageSearchResult");
        public static readonly Uri PackageIdentity = new Uri("http://schema.nuget.org/schema#PackageIdentity");
        public static readonly Uri PackageDescription = new Uri("http://schema.nuget.org/schema#PackageDescription");
        public static readonly Uri PackageLicensing = new Uri("http://schema.nuget.org/schema#PackageLicensing");
        public static readonly Uri PackageDependencies = new Uri("http://schema.nuget.org/schema#PackageDependencies");
        public static readonly Uri DependencyGroup = new Uri("http://schema.nuget.org/schema#DependencyGroup");
        public static readonly Uri Dependency = new Uri("http://schema.nuget.org/schema#Dependency");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Tools
{
    public static class Uris
    {
        public static readonly string Schema = "http://schema.nuget.org/schema#";

        public static class Types
        {
            public static readonly Uri PackageSearchResult = new Uri(Schema + "PackageSearchResult");
        }

        public static class Properties
        {
            public static readonly Uri PackageId = new Uri(Schema + "id");
            public static readonly Uri Version = new Uri(Schema + "version");
            public static readonly Uri PackageVersion = new Uri(Schema + "packageVersion");
            public static readonly Uri Summary = new Uri(Schema + "summary");
            public static readonly Uri IconUrl = new Uri(Schema + "iconUrl");
            public static readonly Uri Description = new Uri(Schema + "description");
            public static readonly Uri Author = new Uri(Schema + "author");
            public static readonly Uri Owner = new Uri(Schema + "owner");
            public static readonly Uri LicenseUrl = new Uri(Schema + "licenseUrl");
            public static readonly Uri ProjectUrl = new Uri(Schema + "projectUrl");
            public static readonly Uri Tags = new Uri(Schema + "tags");
            public static readonly Uri DownloadCount = new Uri(Schema + "downloadCount");
            public static readonly Uri Published = new Uri(Schema + "published");
            public static readonly Uri DependencyGroup = new Uri(Schema + "dependencyGroup");
            public static readonly Uri Dependency = new Uri(Schema + "dependency");
            public static readonly Uri TargetFramework = new Uri(Schema + "targetFramework");
            public static readonly Uri VersionRange = new Uri(Schema + "versionRange");
            public static readonly Uri LatestVersion = new Uri(Schema + "latestVersion");
        }
    }
}

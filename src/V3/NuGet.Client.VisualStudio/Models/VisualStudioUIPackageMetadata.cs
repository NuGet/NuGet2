using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Models
{
    public class VisualStudioUIPackageMetadata 
    {
        string Title { get; set;}
        IEnumerable<string> Authors { get; set;}
        IEnumerable<string> Owners { get; set;}
        Uri IconUrl { get; set;}
        Uri LicenseUrl { get; set;}
        Uri ProjectUrl { get; set;}
        bool RequireLicenseAcceptance { get; set;}
        string Description { get; set;}
        string Summary { get; set;}
        string ReleaseNotes { get; set;}
        string Language { get; set;}
        string Tags { get; set;}
        string Copyright { get; set;}
      //  IEnumerable<PackageDependencySet> DependencySets { get; set;}
        Version MinClientVersion { get; set;}
    }
}

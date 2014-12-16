using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Models
{
    public sealed class VisualStudioUIPackageMetadata 
    {
        public VisualStudioUIPackageMetadata(string title,IEnumerable<string> authors,IEnumerable<string> owners,Uri iconUrl, Uri licenseUrl,Uri projectUrl,bool requiresLiceneseAcceptance,string description,string summary,string releaseNotes,string language,string tags,string copyright,Version minClientVersion)
        {
            Title = title;
            Authors = authors;
            Owners = owners;
            IconUrl = iconUrl;
            ProjectUrl = projectUrl;
            RequireLicenseAcceptance = requiresLiceneseAcceptance;
            Description = description;
            Summary = summary;
            ReleaseNotes = releaseNotes;
            Language = language;
            Tags = tags;
            Copyright = copyright;
            MinClientVersion = minClientVersion;
        }
        string Title { get; private set;}
        IEnumerable<string> Authors { get; private set;}
        IEnumerable<string> Owners { get; private set;}
        Uri IconUrl { get; private set;}
        Uri LicenseUrl { get; private set;}
        Uri ProjectUrl { get; private set;}
        bool RequireLicenseAcceptance { get; private set;}
        string Description { get; private set;}
        string Summary { get; private set;}
        string ReleaseNotes { get; private set;}
        string Language { get; private set;}
        string Tags { get; private set;}
        string Copyright { get; private set;}
        //IEnumerable<PackageDependencySet> DependencySets { get; private set;} *TODOs - copy PackageDependencySet from core to client.baprivate setypes. It has Iversionspec and a whole bunch of things need to be copied or moved.
        Version MinClientVersion { get; private set;}
    }
}

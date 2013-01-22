using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet
{
    [Flags]
    public enum PackageSaveProperties
    {
        None = 0, 
        Nuspec = 1,

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming", 
            "CA1704:IdentifiersShouldBeSpelledCorrectly", 
            MessageId = "Nupkg", 
            Justification = "nupkg is the file extension of the package file")]
        Nupkg = 2        
    }

    public interface IPackageRepository
    {
        string Source { get; }        
        PackageSaveProperties PackageSave { get; set; }
        bool SupportsPrereleasePackages { get; }
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
        IQueryable<IPackage> GetPackages();

        // What files are saved is controlled by property PackageSave.
        void AddPackage(IPackage package);
        void RemovePackage(IPackage package);
    }
}
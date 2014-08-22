using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageDependencyFinder
{
    public static class DependencyFinder
    {
        public static List<ZipPackage> GetPackagesWithWide1stLevelDependencies(List<ZipPackage> packages, int criteria)
        {
            List<ZipPackage> list = new List<ZipPackage>();

            foreach (ZipPackage package in packages)
            {
                List<PackageDependency> firstLevelDependencies = GetFirstLevelDependencyList(package);
                if (firstLevelDependencies.Count() > criteria)
                {
                    list.Add(package);
                    Console.WriteLine(package.Id + " " + package.Version);
                }
            }

            return list;
        }

        public static List<IPackage> GetPackagesWithDeepDependencies(List<ZipPackage> packages, int criteria)
        {
            List<IPackage> list = new List<IPackage>();

            foreach (ZipPackage package in packages)
            {
               
            }

            return list;
        }

        public static int GetDependencyLevelOfPackage(IPackage package, int level)
        {
            IEnumerable<PackageDependencySet> dependencySets = package.DependencySets;
            foreach (PackageDependencySet set in dependencySets)
            {
                ICollection<PackageDependency> coll = set.Dependencies;
                if (coll != null)
                {
                    level++;
                    IPackage pd;
                    foreach (PackageDependency depend in coll)
                    {
                        IVersionSpec spec = depend.VersionSpec;
                        try
                        {
                            pd = GetIPackageFromRepo(depend.Id, spec.MinVersion.ToString());
                        }
                        catch
                        {
                            try
                            {
                                pd = GetIPackageFromRepo(depend.Id, spec.MaxVersion.ToString());
                            }
                            catch
                            {
                                pd = GetZipPackageFromRepo(depend.Id).First();
                            }
                        }
                        if (level > deepestLevel)
                        {
                            deepestLevel = level;
                        }
                        GetDependencyLevelOfPackage(pd, level);
                    }
                }
            }
            return deepestLevel;
        }

        public static List<PackageDependency> GetFirstLevelDependencyList(ZipPackage package)
        {
            List<PackageDependency> firstLevelDependencies = new List<PackageDependency>();
            foreach (PackageDependencySet dependencySet in package.DependencySets)
            {
                firstLevelDependencies.AddRange(dependencySet.Dependencies);
            }
            return firstLevelDependencies;
        }

        public static List<ZipPackage> GetZipPackagesFromPath(string packageRootFolder)
        {
            string[] nupkgFilePaths = Directory.GetFiles(packageRootFolder, "*.nupkg");
            List<ZipPackage> packageList = new List<ZipPackage>();

            foreach (string path in nupkgFilePaths)
            {
                ZipPackage zipPackage = new ZipPackage(path);
                packageList.Add(zipPackage);
            }
            return packageList;
        }
        public static IPackage GetIPackageFromRepo(string id, string version)
        {
            SemanticVersion sVersion;
            SemanticVersion.TryParse(version, out sVersion);
            IPackage package = null; ;

            package = PackageRepo.FindPackage(id, sVersion);
            return package;
        }

        public static IEnumerable<IPackage> GetZipPackageFromRepo(string id)
        {
            IEnumerable<IPackage> packages = PackageRepo.FindPackagesById(id);
            return packages;
        }

        public static IPackageRepository PackageRepo
        {
            get
            {
                _repo = PackageRepositoryFactory.Default.CreateRepository(PackageSource);
                return _repo;
            }
            set
            {
                _repo = value;
            }
        }

        public static string PackageSource
        {
            get
            {
                return _packageSource;
            }
            set
            {
                _packageSource = value;
            }
        }

        private static IPackageRepository _repo;
        private static string _packageSource = "https://www.nuget.org/api/v2/";
        public static int deepestLevel = 0;
    }
}

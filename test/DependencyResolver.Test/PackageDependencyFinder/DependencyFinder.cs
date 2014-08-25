using NuGet;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                int level = GetDependencyLevelOfPackage(package);
                if (level > criteria)
                {
                    list.Add(package);
                }
            }

            return list;
        }

        public static List<IPackage> GetDiamondDependencyPackages(List<ZipPackage> packages)
        {
            List<IPackage> list = new List<IPackage>();


            foreach (ZipPackage package in packages)
            {
                List<PackageDependency> dependenciesFirtLevel = GetFirstLevelDependencyList(package);
                List<PackageDependency> dependenciesSecondLevel = new List<PackageDependency>();

                foreach (PackageDependency depend in dependenciesFirtLevel)
                {
                    IPackage p = ConvertToIPackageFromDependency(depend);
                    List<PackageDependency> deps = GetFirstLevelDependencyList(p as ZipPackage);
                    dependenciesSecondLevel.AddRange(deps);
                }

                // Find idential package Ids in the list of 2nd level dependencies
                int count = dependenciesSecondLevel.Count;
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        if (dependenciesSecondLevel[i].Id.ToLowerInvariant() == dependenciesSecondLevel[j].Id.ToLowerInvariant())   
                        {
                            list.Add(package);
                        }
                    }
                }
            }

            return list;
        }

        public static List<IPackage> GetHardDependencyPackages (List<ZipPackage> packages)
        {
            List<IPackage> list = new List<IPackage>();

            foreach (ZipPackage package in packages)
            {
                List<PackageDependency> dependencies = GetCompletePackageDependencyList(package);
                bool isHardDependencyPackage = true;
                foreach (PackageDependency dep in dependencies)
                {
                    if (!dep.ToString().Contains("="))
                    {
                        isHardDependencyPackage = false;
                    }
                }

                if (isHardDependencyPackage)
                {
                    list.Add(package);
                }
            }

            return list;
        }

        public static List<IPackage> GetSatellitePackages(List<ZipPackage> packages)
        {
            List<string> cultureNames = GetCultureNames(CultureTypes.NeutralCultures);
            List<IPackage> list = new List<IPackage>();

            foreach (ZipPackage package in packages)
            {
                string name = package.Id;
                string lastPart = name.Split(new string[] {"."}, StringSplitOptions.RemoveEmptyEntries).ToList().Last();
               
                foreach (string culture in cultureNames)
                {
                    if (culture == lastPart)
                    {
                        list.Add(package);
                    }
                }
            }
 
            return list;
        }

        public static List<string> GetCultureNames(CultureTypes type)
        {
            List<string> cultures = new List<string>();
            foreach (CultureInfo ci in CultureInfo.GetCultures(type))
            {
                cultures.Add(ci.Name);
            }
            return cultures;
        }

        /// <summary>
        /// Calculate the deepest level of dependency chain for a given package
        /// </summary>
        /// <param name="package">IPackage</param>
        /// <param name="level">starting level</param>
        /// <returns></returns>
        public static int GetDependencyLevelOfPackage(IPackage package, int level = 0)
        {
            IEnumerable<PackageDependencySet> dependencySets = package.DependencySets;
            int deepestLevel = level;
            foreach (PackageDependencySet set in dependencySets)
            {
                ICollection<PackageDependency> coll = set.Dependencies;
                if (coll != null)
                {
                    level++;
                    IPackage pd;
                    foreach (PackageDependency depend in coll)
                    {
                        pd = ConvertToIPackageFromDependency(depend);
                        int n = GetDependencyLevelOfPackage(pd, level);
                        if (n > deepestLevel)
                        {
                            deepestLevel = n;
                        }
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

        public static List<PackageDependency> GetCompletePackageDependencyList(ZipPackage package)
        {
            List<IPackage> list = new List<IPackage>();
            list.Add(package);
            return GetNthLevelOfPackageDependencyList(list, Int16.MaxValue);
        }

        public static List<PackageDependency> GetNthLevelOfPackageDependencyList(List<IPackage> packages, int N, int level = 0)
        {
            List<PackageDependency> dependencies = new List<PackageDependency>();
            List<IPackage> list = new List<IPackage>();
            foreach (IPackage p in packages)
            {
                foreach (PackageDependencySet dependencySet in p.DependencySets)
                {
                    dependencies.AddRange(dependencySet.Dependencies);
                }

                foreach (PackageDependency dependency in dependencies)
                {
                    IPackage pkg = ConvertToIPackageFromDependency(dependency);
                    list.Add(pkg);
                }
            }
            level++;
            if (level < N || N == Int16.MaxValue)
            {
                GetNthLevelOfPackageDependencyList(list, N, level);
            }
            return dependencies;
        }

        /// <summary>
        /// Based on the PackageDependency, try match a package from the repro.
        /// </summary>
        /// <param name="dependency"></param>
        /// <returns>IPackage</returns>
        public static IPackage ConvertToIPackageFromDependency(PackageDependency dependency)
        {
            IPackage package;
            IVersionSpec spec = dependency.VersionSpec;
            try
            {
                package = GetIPackageFromRepo(dependency.Id, spec.MinVersion.ToString());
            }
            catch
            {
                try
                {
                    package = GetIPackageFromRepo(dependency.Id, spec.MaxVersion.ToString());
                }
                catch
                {
                    package = GetZipPackageFromRepo(dependency.Id).First();
                }
            }
            return package;
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
    }
}

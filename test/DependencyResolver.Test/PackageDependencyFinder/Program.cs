using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageDependencyFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            //List<ZipPackage> list = DependencyFinder.GetZipPackagesFromPath(@"F:\dd\git\nugetcx\test\DependencyResolution.Test1\NupkgsTop790Packages");
            //DependencyFinder.GetPackagesWithWide1stLevelDependencies(list, 10);
            //DependencyFinder.GetPackagesWithDeepDependencies(list, 5);
            IPackage package = DependencyFinder.GetIPackageFromRepo("microsoft.aspnet.mvc", "5.0.0");
            IPackage package1 = DependencyFinder.GetIPackageFromRepo("Microsoft.AspNet.WebApi", "5.2.0");
            IPackage package2 = DependencyFinder.GetIPackageFromRepo("Dotnetopenauth.aspnet", "4.3.4.13329");
            int level = DependencyFinder.GetDependencyLevelOfPackage(package);
            DependencyFinder.deepestLevel = 0;
            int level1 = DependencyFinder.GetDependencyLevelOfPackage(package1);
            DependencyFinder.deepestLevel = 0;
            int level2 = DependencyFinder.GetDependencyLevelOfPackage(package2);
        }
    }
}

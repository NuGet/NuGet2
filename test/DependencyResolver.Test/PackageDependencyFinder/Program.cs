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
            List<ZipPackage> list = DependencyFinder.GetZipPackagesFromPath(@"F:\dd\git\nugetcx\test\DependencyResolution.Test1\NupkgsTop790Packages");
            //DependencyFinder.GetPackagesWithWide1stLevelDependencies(list, 10);
            DependencyFinder.GetPackagesWithDeepDependencies(list, 5);
        }
    }
}

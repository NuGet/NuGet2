using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.V2;
using NuGet.Client.VisualStudio.Repository;
using NuGet.Client.VisualStudio;
using NuGet.Client;
using NuGet;

namespace TempTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            NuGet.Client.PackageSource source = new NuGet.Client.PackageSource("nuget.org","https://nuget.org/api/v2");
            IPackageRepository repo = new DataServicePackageRepository(new RedirectedHttpClient(new Uri("https://nuget.org/api/v2")));
            V2SourceRepository2 repo2 = new V2SourceRepository2(source, repo, "xx");
            Console.WriteLine(repo2.v2Resource.Count());
            
        }
    }
}

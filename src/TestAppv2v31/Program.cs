using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using NuGet.Client.BaseTypes;
using NuGet.Client;
using NuGet.Client.VisualStudio;
using NuGet.Client.VisualStudio.Models;

namespace TestAppv2v31
{
    class Program
    {

        public  void AssembleCalculatorComponents()
        {
            try
            {
                //Creating an instance of aggregate catalog. It aggregates other catalogs
                var aggregateCatalog = new AggregateCatalog();

                //Build the directory path where the parts will be available
                var directoryPath = @"C:\Client\nuget\src\TestAppv2v31\bin\Debug";
                var directoryCatalog = new DirectoryCatalog(directoryPath, "*.dll");              
                aggregateCatalog.Catalogs.Add(directoryCatalog);              
                var container = new CompositionContainer(aggregateCatalog);               
                container.ComposeParts(this);
                IEnumerable<Lazy<SourceRepository>> repos = container.GetExports<SourceRepository>();
                IEnumerable<Lazy<Resource>> resources = container.GetExports<Resource>();                
                PackageSource source = new PackageSource("nuget.org", "https://nuget.org/api/v2");               
                if(repos.Any( p => p.Value.TryGetRepository(source)))
                {
                    //   SourceRepository class has a TryGetRepository and GetRepository which will implemented accordingly by V2SourceRepository and V3SourceRepository derived classed that returns V2Repo and V3Repo respectively.             
                    SourceRepository repo = repos.FirstOrDefault(p => p.Value.TryGetRepository(source)).Value.GetRepository(source);                  
                    VsSearchResource vsSearch = repo.GetRequiredResource<VsSearchResource>();
                    SearchFilter filter = new SearchFilter(); //create a dummy filter.
                    IEnumerable<VisualStudioUISearchMetaData> searchResults = vsSearch.GetSearchResultsForVisualStudioUI("Elmah", filter, 0, 100, new System.Threading.CancellationToken()).Result;
                }
                
               
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        static void Main(string[] args)
        {
            Program p = new Program();
            p.AssembleCalculatorComponents();
            
        }
    }
}

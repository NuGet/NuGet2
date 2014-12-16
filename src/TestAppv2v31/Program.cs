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
using System.Diagnostics;
using System.Runtime.Versioning;

namespace TestAppv2v31
{
    class Program
    {
        private CompositionContainer container;
        public  void AssembleComponents()
        {
            try
            {
                //Creating an instance of aggregate catalog. It aggregates other catalogs
                var aggregateCatalog = new AggregateCatalog();

                //Build the directory path where the parts will be available
                var directoryPath = @"C:\Client\nuget\src\TestAppv2v31\bin\Debug";
                var directoryCatalog = new DirectoryCatalog(directoryPath, "*.dll");              
                aggregateCatalog.Catalogs.Add(directoryCatalog);              
                container = new CompositionContainer(aggregateCatalog);               
                container.ComposeParts(this);
         
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void TestResourceTypeBasedOnPackageSource()
        {
            IEnumerable<Lazy<IResourceProvider, IResourceProviderMetadata>> providers = container.GetExports<IResourceProvider, IResourceProviderMetadata>();
            Debug.Assert(providers.Count() > 0);
            PackageSource source = new PackageSource("nuget.org", "https://nuget.org/api/v2");
            SourceRepository2 repo = new SourceRepository2(source, providers);
            IDownloadResource resource = (IDownloadResource)repo.GetResource<IDownloadResource>();
            Debug.Assert(resource != null);
            Debug.Assert(resource.GetType() == typeof(IDownloadResource));            
        }



        public void TestGetResourceGivesRequiredResourceType()
        {
            IEnumerable<Lazy<IResourceProvider, IResourceProviderMetadata>> providers = container.GetExports<IResourceProvider, IResourceProviderMetadata>();
            Debug.Assert(providers.Count() > 0);
            PackageSource source = new PackageSource("nuget.org", "https://nuget.org/api/v2");
            SourceRepository2 repo = new SourceRepository2(source, providers);
            IDownloadResource resource = (IDownloadResource)repo.GetResource<IDownloadResource>();
            Debug.Assert(resource != null);
            Debug.Assert(resource.GetType().GetInterfaces().Contains(typeof(IDownloadResource)));
        }
        public void TestCachingWorks()
        {
            IEnumerable<Lazy<IResourceProvider, IResourceProviderMetadata>> providers = container.GetExports<IResourceProvider, IResourceProviderMetadata>();
            Debug.Assert(providers.Count() > 0);
            PackageSource source = new PackageSource("nuget.org", "https://nuget.org/api/v2");
            SourceRepository2 repo = new SourceRepository2(source, providers);
            IDownloadResource resource = (IDownloadResource)repo.GetResource<IDownloadResource>();
            Debug.Assert(resource != null);
            Debug.Assert(resource.GetType() == typeof(IDownloadResource));

           
            source = new PackageSource("localcache", @"C:\client");
            repo = new SourceRepository2(source, providers);
            resource = (IDownloadResource)repo.GetResource<IDownloadResource>();
            Debug.Assert(resource != null);
            Debug.Assert(resource.GetType() == typeof(IDownloadResource));
        }
        public void TestE2E()
        {
             IEnumerable<Lazy<IResourceProvider, IResourceProviderMetadata>> providers = container.GetExports<IResourceProvider, IResourceProviderMetadata>();
            Debug.Assert(providers.Count() > 0);
            PackageSource source = new PackageSource("nuget.org", "https://nuget.org/api/v2");
            SourceRepository2 repo = new SourceRepository2(source, providers);
            VsSearchResource resource = (VsSearchResource)repo.GetResource<VsSearchResource>();
            Debug.Assert(resource != null);
            Debug.Assert(resource.GetType().GetInterfaces().Contains(typeof(VsSearchResource)));
            SearchFilter filter = new SearchFilter(); //create a dummy filter.
            List<FrameworkName> fxNames = new List<FrameworkName>();
            fxNames.Add(new FrameworkName(".NET Framework, Version=4.0"));
            filter.SupportedFrameworks = fxNames;                       
            IEnumerable<VisualStudioUISearchMetaData> searchResults = resource.GetSearchResultsForVisualStudioUI("Elmah", filter, 0, 100, new System.Threading.CancellationToken()).Result;
            Debug.Assert(searchResults.Count() > 0); // Check if non empty search result is returned.
            Debug.Assert(searchResults.Any(p => p.Id.Equals("Elmah", StringComparison.OrdinalIgnoreCase))); //check if there is an result which has Elmah as title.
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.AssembleComponents();
          //  p.TestGetResourceGivesRequiredResourceType();
           // p.TestCachingWorks();
            p.TestE2E();
            
        }
    }
}

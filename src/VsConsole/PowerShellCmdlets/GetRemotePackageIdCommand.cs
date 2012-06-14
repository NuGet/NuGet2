using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "RemotePackageId")]
    [OutputType(typeof(string))]
    public class GetRemotePackageIdCommand : JsonApiCommandBase<string>
    {
        public GetRemotePackageIdCommand()
            : this(
			    ServiceLocator.GetInstance<ISolutionManager>(),
			    ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
			    ServiceLocator.GetInstance<IHttpClientEvents>(),
                ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
			    ServiceLocator.GetInstance<IVsPackageSourceProvider>())
        {
        }

        public GetRemotePackageIdCommand(
		    ISolutionManager solutionManager,
		    IVsPackageManagerFactory packageManagerFactory,
            IHttpClientEvents httpClientEvents,
            IPackageRepositoryFactory repositoryFactory,
		    IVsPackageSourceProvider packageSourceProvider)
            : base(solutionManager, packageManagerFactory, httpClientEvents, repositoryFactory, packageSourceProvider)
	    {
	    }
        
        public override string ApiEndpointPath { get { return "api/v2/package-ids"; } }
        
        [Parameter]
	    [ValidateNotNullOrEmpty]
	    public string Filter { get; set; }

        protected override Dictionary<string, string> BuildApiEndpointQueryParameters()
        {
            var queryParameters = new Dictionary<string, string>();
            if (!String.IsNullOrWhiteSpace(Filter))
            {
                queryParameters.Add("partialId", Filter);
            }

            return queryParameters;
        }

        protected override IEnumerable<string> GetResultsFromPackageRepository(IPackageRepository packageRepository)
        {
            IEnumerable<IPackage> packages = packageRepository.GetPackages();
            
            if (!String.IsNullOrEmpty(Filter))
            {
                packages = packages.Where(p => p.Id.StartsWith(Filter, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!IncludePrerelease)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            return packages.Select(p => p.Id)
                .Distinct()
                .Take(30);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "RemotePackageVersion")]
	[OutputType(typeof(string))]
    public class GetRemotePackageVersionCommand : JsonApiCommandBase<string>
    {
        public GetRemotePackageVersionCommand()
            : this(
				ServiceLocator.GetInstance<ISolutionManager>(),
				ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
				ServiceLocator.GetInstance<IHttpClientEvents>(),
                ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
				ServiceLocator.GetInstance<IVsPackageSourceProvider>())
        {
        }

        public GetRemotePackageVersionCommand(
			ISolutionManager solutionManager,
			IVsPackageManagerFactory packageManagerFactory,
            IHttpClientEvents httpClientEvents,
            IPackageRepositoryFactory repositoryFactory,
			IVsPackageSourceProvider packageSourceProvider)
            : base(solutionManager, packageManagerFactory, httpClientEvents, repositoryFactory, packageSourceProvider)
		{
		}
        
        public override string ApiEndpointPath
        {
            get
            {
                Debug.Assert(Id != null, "Id is a mandatory parameter and should never be null.");
                return string.Format(CultureInfo.InvariantCulture, "api/v2/package-versions/{0}", Id);
            }
        }
        
        [Parameter(Position = 0, Mandatory = true)]
		[ValidateNotNullOrEmpty]
		public string Id { get; set; }

        protected override Dictionary<string, string> BuildApiEndpointQueryParameters()
        {
            return null;
        }

        protected override IEnumerable<string> GetResultsFromPackageRepository(IPackageRepository packageRepository)
        {
            var packages = packageRepository.GetPackages().Where(p => p.Id.Equals(Id, StringComparison.OrdinalIgnoreCase));

            if (!IncludePrerelease)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            return packages.Select(p => p.Version.ToString());
        }
    }
}
using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.Serialization.Json;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "PackageId")]
	[OutputType(typeof(string))]
    public class GetPackageIdCommand : NuGetBaseCommand
    {
    	private readonly IVsPackageSourceProvider _packageSourceProvider;

    	public GetPackageIdCommand()
            : this(
				ServiceLocator.GetInstance<ISolutionManager>(),
				ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
				ServiceLocator.GetInstance<IHttpClientEvents>(),
				ServiceLocator.GetInstance<IVsPackageSourceProvider>())
        {
        }

		public GetPackageIdCommand(
			ISolutionManager solutionManager,
			IVsPackageManagerFactory packageManagerFactory,
            IHttpClientEvents httpClientEvents,
			IVsPackageSourceProvider packageSourceProvider)
            : base(solutionManager, packageManagerFactory, httpClientEvents)
		{
			_packageSourceProvider = packageSourceProvider;
		}

    	[Parameter(Position = 0)]
		[ValidateNotNullOrEmpty]
		public string Filter { get; set; }

		[Parameter(Position = 1)]
		[ValidateNotNullOrEmpty]
		public string Source { get; set; }

		[Parameter]
		[Alias("Prerelease")]
		public SwitchParameter IncludePrerelease { get; set; }

		protected override void ProcessRecordCore()
		{
            foreach (var packageId in GetPackageIds(GetUri()))
            {
                if (Stopping)
                    break;
                WriteObject(packageId);
            }
		}

		protected virtual string[] GetPackageIds(Uri uri)
		{
            var jsonSerializer = new DataContractJsonSerializer(typeof(string[]));
            var httpClient = new HttpClient(uri);
            using (var stream = new MemoryStream(httpClient.DownloadData()))
            {
                return jsonSerializer.ReadObject(stream) as string[];
            }
		}
        
        protected virtual Uri GetUri()
		{
			string baseUri;
			if (!String.IsNullOrEmpty(Source))
				baseUri = new Uri(Source).GetLeftPart(UriPartial.Path);
            else if (_packageSourceProvider.ActivePackageSource != null)
				baseUri = new Uri(_packageSourceProvider.ActivePackageSource.Source).GetLeftPart(UriPartial.Authority);
            else
                throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);

			var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
			if (!string.IsNullOrWhiteSpace(Filter))
				queryString["partialId"] = Filter;
			if (IncludePrerelease)
				queryString["includePrerelease"] = "true";

			return new Uri(baseUri + "/api/v2/package-ids?" + queryString);
        }
    }
}
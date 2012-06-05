using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.Serialization.Json;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "PackageVersion")]
	[OutputType(typeof(string))]
    public class GetPackageVersionCommand : NuGetBaseCommand
    {
    	private readonly IVsPackageSourceProvider _packageSourceProvider;

    	public GetPackageVersionCommand()
            : this(
				ServiceLocator.GetInstance<ISolutionManager>(),
				ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
				ServiceLocator.GetInstance<IHttpClientEvents>(),
				ServiceLocator.GetInstance<IVsPackageSourceProvider>())
        {
        }

		public GetPackageVersionCommand(
			ISolutionManager solutionManager,
			IVsPackageManagerFactory packageManagerFactory,
            IHttpClientEvents httpClientEvents,
			IVsPackageSourceProvider packageSourceProvider)
            : base(solutionManager, packageManagerFactory, httpClientEvents)
		{
			_packageSourceProvider = packageSourceProvider;
		}

    	[Parameter(Position = 0, Mandatory = true)]
		[ValidateNotNullOrEmpty]
		public string Id { get; set; }

		[Parameter(Position = 1)]
		[ValidateNotNullOrEmpty]
		public string Source { get; set; }

		[Parameter]
		[Alias("Prerelease")]
		public SwitchParameter IncludePrerelease { get; set; }

		protected override void ProcessRecordCore()
		{
			var jsonSerializer = new DataContractJsonSerializer(typeof(string[]));
			var httpClient = new HttpClient(GetUri());
			string[] packageVersions;
			using (var stream = new MemoryStream(httpClient.DownloadData()))
			{
				packageVersions = jsonSerializer.ReadObject(stream) as string[];
			}
			WritePackageIds(packageVersions);
        }

		private Uri GetUri()
		{
			string baseUri;
			if (!String.IsNullOrEmpty(Source))
				baseUri = new Uri(Source).GetLeftPart(UriPartial.Path);
            else if (_packageSourceProvider.ActivePackageSource != null)
				baseUri = new Uri(_packageSourceProvider.ActivePackageSource.Source).GetLeftPart(UriPartial.Authority);
            else
                throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);

			var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
			if (IncludePrerelease)
				queryString["includePrerelease"] = "true";

			return new Uri(string.Format("{0}/api/v2/package-versions/{1}?{2}", baseUri, Id, queryString));
        }

		private void WritePackageIds(IEnumerable<string> packageIds)
        {
			foreach (var packageId in packageIds)
            {
                if (Stopping)
                    break;
				WriteObject(packageId);
            }
        }
    }
}
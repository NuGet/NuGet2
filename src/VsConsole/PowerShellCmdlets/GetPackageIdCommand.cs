using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			var sw = new Stopwatch();
			sw.Start();
			var jsonSerializer = new DataContractJsonSerializer(typeof(string[]));
			var httpClient = new HttpClient(GetUri());
			string[] packageIds;
			using (var stream = new MemoryStream(httpClient.DownloadData()))
			{
				packageIds = jsonSerializer.ReadObject(stream) as string[];
			}
			WritePackageIds(packageIds);
			sw.Stop();
			Debug.WriteLine("Process took " + sw.Elapsed.TotalMilliseconds);
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
			if (!string.IsNullOrWhiteSpace(Filter))
				queryString["partialId"] = Filter;
			if (IncludePrerelease)
				queryString["includePrerelease"] = "true";

			return new Uri(baseUri + "/api/v2/package-ids?" + queryString);
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
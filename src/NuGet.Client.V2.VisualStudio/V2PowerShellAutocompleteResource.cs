using NuGet.Client.V2;
using NuGet.Client.VisualStudio;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;


namespace NuGet.Client.V2.VisualStudio
{
    public class V2PowerShellAutoCompleteResource : PSAutoCompleteResource
    {
        private readonly IPackageRepository V2Client;
        public V2PowerShellAutoCompleteResource(V2Resource resource)          
        {
            V2Client = resource.V2Client;
        }
        public V2PowerShellAutoCompleteResource(IPackageRepository repo)
        {
            V2Client = repo;
        }
        public override Task<IEnumerable<string>> IdStartsWith(string packageIdPrefix, bool includePrerelease, System.Threading.CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                //*TODOs:In existing JsonApiCommandBase the validation done to find if the source is local or not is "IsHttpSource()"... Which one is better to use ?
                LocalPackageRepository lrepo = V2Client as LocalPackageRepository;
                if (lrepo != null)
                {
                    return GetPackageIdsFromLocalPackageRepository(lrepo, packageIdPrefix, true);
                }
                else
                {
                    return GetPackageIdsFromHttpSourceRepository(V2Client, packageIdPrefix, true);
                }
            });
        }

        public override Task<IEnumerable<Versioning.NuGetVersion>> VersionStartsWith(string packageId, string versionPrefix, bool includePrerelease, System.Threading.CancellationToken token)
        {
            throw new NotImplementedException();
        }
    

        private static IEnumerable<string> GetPackageIdsFromHttpSourceRepository(IPackageRepository packageRepository,string searchFilter,bool includePrerelease)
        {
            var packageSourceUri = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/", packageRepository.Source.TrimEnd('/')));
            var apiEndpointUri = new UriBuilder(new Uri(packageSourceUri, @"api/v2/package-ids"))
            {
                Query = "partialId=" + searchFilter + "&" + "includePrerelease=" + includePrerelease.ToString()
            };
            return GetResults(apiEndpointUri.Uri);
        }

        private static IEnumerable<string> GetPackageIdsFromLocalPackageRepository(IPackageRepository packageRepository,string searchFilter,bool includePrerelease)
        {
            IEnumerable<IPackage> packages = packageRepository.GetPackages();

            if (!String.IsNullOrEmpty(searchFilter))
            {
                packages = packages.Where(p => p.Id.StartsWith(searchFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!includePrerelease)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            return packages.Select(p => p.Id)
                .Distinct()
                .Take(30);
        }

        private static IEnumerable<string> GetResults(Uri apiEndpointUri)
        {
            var jsonSerializer = new DataContractJsonSerializer(typeof(string[]));
            var httpClient = new HttpClient(apiEndpointUri);
            using (var stream = new MemoryStream())
            {
                httpClient.DownloadData(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return jsonSerializer.ReadObject(stream) as string[];
            }
        }

    }
}

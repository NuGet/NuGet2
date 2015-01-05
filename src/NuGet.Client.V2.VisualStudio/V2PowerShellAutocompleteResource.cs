using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.VisualStudio.Models;
using NuGet.Client.V2;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;

namespace NuGet.Client.V2.VisualStudio
{
    public class V2PowerShellAutoCompleteResource : V2Resource, IPowerShellAutoComplete
    {
        public V2PowerShellAutoCompleteResource(V2Resource resource)
            : base(resource)
        {
        }
        public Task<IEnumerable<Versioning.NuGetVersion>> GetAllVersions(string versionPrefix)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetPackageIdsStartingWith(string packageIdPrefix, System.Threading.CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
          {
              //*TODOs:In existing JsonApiCommandBase the validation done to find if the source is local or not is "IsHttpSource()"... Which one is better to use ?
              LocalPackageRepository lrepo = V2Client as LocalPackageRepository;
              if(lrepo != null)
              {
                  return GetPackageIdsFromLocalPackageRepository(lrepo, packageIdPrefix, true);
              }
              else
              {
                  return GetPackageIdsFromHttpSourceRepository(V2Client, packageIdPrefix, true);
              }
          });
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

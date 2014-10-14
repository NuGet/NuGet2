using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Data;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonLD.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NuGet.Client
{
    public class V3SourceRepository : SourceRepository
    {
        private DataClient _client;
        private PackageSource _source;
        private Uri _root;
        private static readonly NuGetVersion DefaultVersion = new NuGetVersion("2.0.0");
        private static readonly VersionRange ServiceVersionRange = new VersionRange(
            minVersion: new NuGetVersion("3.0.0-preview.1"),
            maxVersion: new NuGetVersion("3.0.0-preview.1"),
            includeMaxVersion: true,
            includeMinVersion: true);

        private static readonly Uri[] ResultItemRequiredProperties = new Uri[] {
            new Uri("http://schema.nuget.org/schema#registration")
        };

        private static readonly Uri[] PackageRequiredProperties = new Uri[] {
            new Uri("http://schema.nuget.org/schema#catalogEntry")
        };

        private static readonly Uri[] CatalogRequiredProperties = new Uri[] {
            new Uri("http://schema.nuget.org/schema#items")
        };

        private static readonly Uri[] PackageDetailsRequiredProperties = new Uri[] {
            new Uri("http://schema.nuget.org/schema#authors"),
            new Uri("http://schema.nuget.org/schema#description"),
            new Uri("http://schema.nuget.org/schema#iconUrl"),
            new Uri("http://schema.nuget.org/schema#id"),
            new Uri("http://schema.nuget.org/schema#language"),
            new Uri("http://schema.nuget.org/schema#licenseUrl"),
            new Uri("http://schema.nuget.org/schema#minClientVersion"),
            new Uri("http://schema.nuget.org/schema#projectUrl"),
            new Uri("http://schema.nuget.org/schema#published"),
            new Uri("http://schema.nuget.org/schema#requireLicenseAcceptance"),
            new Uri("http://schema.nuget.org/schema#summary"),
            new Uri("http://schema.nuget.org/schema#tags"),
            new Uri("http://schema.nuget.org/schema#title"),
            new Uri("http://schema.nuget.org/schema#version"),
        };

        public override PackageSource Source
        {
            get { return _source; }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The HttpClient can be left open until VS shuts down.")]
        public V3SourceRepository(PackageSource source)
        {
            _source = source;
            _root = new Uri(source.Url);
            _client = new DataClient(
                new System.Net.Http.HttpClient(),
                new BrowserFileCache(),
                context: null);
        }

        public async override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {
            // Get the search service URL from the service
            cancellationToken.ThrowIfCancellationRequested();
            var searchService = await GetServiceUri(ServiceUris.SearchQueryService, ServiceVersionRange);
            if (String.IsNullOrEmpty(searchService))
            {
                throw new NuGetProtocolException(Strings.Protocol_MissingSearchService);
            }
            cancellationToken.ThrowIfCancellationRequested();

            // Construct the query
            var queryUrl = new UriBuilder(searchService);
            string queryString =
                "q=" + searchTerm +
                "&skip=" + skip.ToString() +
                "&take=" + take.ToString() +
                "&includePrerelease=" + filters.IncludePrerelease.ToString().ToLowerInvariant();
            string frameworks = 
                String.Join("&", 
                    filters.SupportedFrameworks.Select(
                        fx => "supportedFramework=" + VersionUtility.GetShortFrameworkName(fx)));

            if (!String.IsNullOrEmpty(frameworks))
            {
                queryString += "&" + frameworks;
            }
            queryUrl.Query = queryString;

            // Execute the query! Bypass the cache for now
            NuGetTraceSources.V3SourceRepository.Info(
                "searching",
                "Executing Query: {0}",
                queryUrl.ToString());
            var results = await _client.GetFile(queryUrl.Uri);
            cancellationToken.ThrowIfCancellationRequested();
            if (results == null)
            {
                NuGetTraceSources.V3SourceRepository.Warning(
                    "results_invalid",
                    "Recieved unexpected results from {0}!",
                    queryUrl.ToString());
                return Enumerable.Empty<JObject>();
            }
            var data = results.Value<JArray>("data");
            if (data == null)
            {
                NuGetTraceSources.V3SourceRepository.Warning(
                    "results_invalid",
                    "Recieved invalid results from {0}!",
                    queryUrl.ToString());
                return Enumerable.Empty<JObject>();
            }
            
            NuGetTraceSources.V3SourceRepository.Verbose(
                "results_received",
                "Received {1} hits from {0}",
                queryUrl.ToString(),
                data.Count);

            // Resolve all the objects
            List<JObject> outputs = new List<JObject>(take);
            foreach (var result in data.Take(take).Cast<JObject>())
            {
                outputs.Add(await ProcessSearchResult(cancellationToken, result));
            }

            //var input = data.Take(take).Cast<JObject>().ToList();
            //JObject[] outputs = new JObject[input.Count];

            //// The search service might actually return more than `take` items :)
            //Parallel.ForEach(input, (result, state, index) =>
            //{
            //    outputs[index] = ProcessSearchResult(cancellationToken, result).Result;
            //});

            return outputs;
        }

        private async Task<JObject> ProcessSearchResult(System.Threading.CancellationToken cancellationToken, JObject result)
        {
            NuGetTraceSources.V3SourceRepository.Verbose(
                                "resolving_package",
                                "Resolving Package: {0}",
                                result[Properties.SubjectId]);

            // Get the registration
            result = (JObject)(await _client.Ensure(result, ResultItemRequiredProperties));
            var registrationUrl = new Uri(result["registration"].ToString());

            // Fetch the registration
            NuGetTraceSources.V3SourceRepository.Verbose(
                "resolving_registration",
                "Resolving Package Registration: {0}",
                registrationUrl);
            var registration = await _client.GetEntity(registrationUrl);

            // Descend through the pages until we find all Packages
            var packages = await Descend((JArray)registration["items"]);

            // Find the recommended package
            var primaryResult = packages.FirstOrDefault(
                p => NuGetVersion.Parse(p["catalogEntry"]["version"].ToString()).Equals(NuGetVersion.Parse(result["version"].ToString())));

            // Construct a result object
            var searchResult = new JObject()
            {
                {Properties.PackageId, primaryResult["catalogEntry"][Properties.PackageId] },
                {Properties.LatestVersion, primaryResult["catalogEntry"][Properties.Version] },
                {Properties.Summary, primaryResult["catalogEntry"][Properties.Summary] },
                {Properties.IconUrl, primaryResult["catalogEntry"][Properties.IconUrl] }
            };

            // Fill in the package entries
            var versions = new JArray();
            foreach (var version in packages)
            {
                NuGetTraceSources.V3SourceRepository.Verbose(
                    "resolving_version",
                    "Resolving Package Version: {0}",
                    version[Properties.SubjectId]);
                version["catalogEntry"][Properties.PackageContent] = version[Properties.PackageContent];
                versions.Add(version["catalogEntry"]);
            }
            searchResult[Properties.Packages] = versions;

            return searchResult;
        }

        public override async Task<JObject> GetPackageMetadata(string id, NuGetVersion version)
        {
            return (await GetPackageMetadataById(id))
                .FirstOrDefault(p => String.Equals(p["version"].ToString(), version.ToNormalizedString(), StringComparison.OrdinalIgnoreCase));
        }

        public override async Task<IEnumerable<JObject>> GetPackageMetadataById(string packageId)
        {
            // Get the base URL
            var baseUrl = await GetServiceUri(ServiceUris.RegistrationsBaseUrl, ServiceVersionRange);
            if (String.IsNullOrEmpty(baseUrl))
            {
                throw new NuGetProtocolException(Strings.Protocol_MissingRegistrationBase);
            }

            // Construct the URL
            var packageUrl = baseUrl.TrimEnd('/') + "/" + packageId.ToLowerInvariant() + "/index.json";

            // Resolve the catalog root
            var catalogPackage = await _client.Ensure(await _client.GetEntity(new Uri(packageUrl)), CatalogRequiredProperties);

            // Descend through the items to find all the versions
            var versions = await Descend((JArray)catalogPackage["items"]);

            // Return the catalogEntry values
            return versions.Select(o => {
                var result = (JObject)o["catalogEntry"];
                result[Properties.PackageContent] = o[Properties.PackageContent];
                return result;
            });
        }

        private async Task<IEnumerable<JObject>> Descend(JArray json)
        {
            List<IEnumerable<JObject>> lists = new List<IEnumerable<JObject>>();
            List<JObject> items = new List<JObject>();
            lists.Add(items);
            foreach (var item in json)
            {
                string type = item["@type"].ToString();
                if (Equals(type, "catalog:CatalogPage"))
                {
                    var resolved = await _client.Ensure(item, new[] {
                        new Uri("http://schema.nuget.org/schema#items")
                    });
                    Debug.Assert(resolved != null, "DataClient returned null from Ensure :(");
                    lists.Add(await Descend((JArray)resolved["items"]));
                }
                else if(Equals(type, "Package"))
                {
                    // Yield this item with catalogEntry and it's subfields ensured
                    var resolved = await _client.Ensure(item, PackageRequiredProperties);
                    resolved["catalogEntry"] = await _client.Ensure(resolved["catalogEntry"], PackageDetailsRequiredProperties);
                    items.Add((JObject)resolved);
                }
            }

            // Flatten the list and return it
            return lists.SelectMany(j => j);
        }

        private async Task<string> GetServiceUri(Uri type, VersionRange requiredVersionRange)
        {
            // Read the root document (usually out of the cache :))
            var doc = await _client.GetFile(new Uri(_source.Url));
            var obj = JsonLdProcessor.Expand(doc).FirstOrDefault();
            if (obj == null)
            {
                throw new NuGetProtocolException(Strings.Protocol_IndexMissingResourcesNode);
            }
            var resources = obj[ServiceUris.Resources.ToString()] as JArray;
            if (resources == null)
            {
                throw new NuGetProtocolException(Strings.Protocol_IndexMissingResourcesNode);
            }

            // Query it for the requested service
            var candidates = (from resource in resources.OfType<JObject>()
                              let resourceType = resource["@type"].Select(t => t.ToString()).FirstOrDefault()
                              let resourceVersion = resource
                                    .Value<JArray>(ServiceUris.Version.ToString())
                                    .Select(v => v.Value<string>("@value"))
                                    .FirstOrDefault()
                              where resourceType != null && Equals(resourceType, type.ToString())
                              let parsedVersion = (resourceVersion == null ? null : NuGetVersion.Parse(resourceVersion.ToString()))
                                 ?? DefaultVersion
                              where requiredVersionRange.Satisfies(parsedVersion)
                              select resource)
                             .ToList();
            NuGetTraceSources.V3SourceRepository.Verbose(
                "service_candidates",
                "Found {0} candidates for {1} ({2}) service: [{3}]",
                candidates.Count,
                type,
                requiredVersionRange,
                String.Join(", ", candidates.Select(c => c.Value<string>("@id"))));

            var selected = candidates.FirstOrDefault();

            if (selected != null)
            {
                NuGetTraceSources.V3SourceRepository.Info(
                    "getserviceuri",
                    "Found {0} {1} service at {2}",
                    selected["@type"][0],
                    selected[ServiceUris.Version.ToString()][0]["@value"],
                    selected["@id"]);
                return selected.Value<string>("@id");
            }
            else
            {
                NuGetTraceSources.V3SourceRepository.Error(
                    "getserviceuri_failed",
                    "Unable to find compatible {0} service (range {1}) on {2}",
                    type,
                    requiredVersionRange,
                    _root);
                return null;
            }
        }
    }
}

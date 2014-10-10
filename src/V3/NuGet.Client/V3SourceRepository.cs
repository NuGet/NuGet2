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

namespace NuGet.Client
{
    public class V3SourceRepository : SourceRepository
    {
        private object _lock = new object();

        private DataClient _client;
        private PackageSource _source;
        private Uri _root;
        private static readonly NuGetVersion DefaultVersion = new NuGetVersion("2.0.0");
        private static readonly VersionRange ServiceVersionRange = new VersionRange(
            minVersion: new NuGetVersion("3.0.0-preview.1"),
            maxVersion: new NuGetVersion("3.0.0-preview.1"),
            includeMaxVersion: true,
            includeMinVersion: true);

        private static readonly Uri[] PackageRequiredProperties = new Uri[] {
            new Uri("http://schema.nuget.org/schema#catalogEntry")
        };

        private static readonly Uri[] CatalogPackageRequiredProperties = new Uri[] {
            new Uri("http://schema.nuget.org/schema#id"),
            new Uri("http://schema.nuget.org/schema#nupkgUrl"),
            new Uri("http://schema.nuget.org/schema#version")
        };

        public override PackageSource Source
        {
            get { return _source; }
        }

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
            cancellationToken.ThrowIfCancellationRequested();

            // Construct the query
            var queryUrl = new UriBuilder(searchService);
            queryUrl.Query =
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
                queryUrl.Query += "&" + frameworks;
            }

            // Execute the query! Bypass the cache for now
            NuGetTraceSources.V3SourceRepository.Info(
                "searching",
                "Executing Query: {0}",
                queryUrl.ToString());
            var results = await _client.GetFile(queryUrl.Uri);
            cancellationToken.ThrowIfCancellationRequested();
            results[Properties.SubjectId] = queryUrl.Uri.ToString();
            results["url"] = results[Properties.SubjectId];
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
            List<JObject> resolvedResults = new List<JObject>();
            foreach (var result in data)
            {
                // Get the full blob
                var package = (JObject)(await _client.Ensure(result, PackageRequiredProperties));
                cancellationToken.ThrowIfCancellationRequested();
                var catalogPackage = package["catalogEntry"];
                var resolvedPackage = (JObject)(await _client.Ensure(catalogPackage, CatalogPackageRequiredProperties));

                // Construct a result object
                resolvedResults.Add(new JObject()
                {
                    {Properties.PackageId, resolvedPackage[Properties.PackageId] },
                    {Properties.LatestVersion, resolvedPackage[Properties.Version] },
                    {Properties.Summary, resolvedPackage[Properties.Summary] },
                    {Properties.IconUrl, resolvedPackage[Properties.IconUrl] },
                    {Properties.Packages, new JArray(
                        new JObject() {
                            {Properties.PackageId, resolvedPackage[Properties.PackageId]},
                            {Properties.Version, resolvedPackage[Properties.Version]},
                            {Properties.Summary, resolvedPackage[Properties.Summary]},
                            {Properties.Description, resolvedPackage[Properties.Description]}
                        }
                    ) }
                });
            }
            return resolvedResults;
        }

        public override Task<JObject> GetPackageMetadata(string id, NuGetVersion version)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<JObject>> GetPackageMetadataById(string packageId)
        {
            throw new NotImplementedException();
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

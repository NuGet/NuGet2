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
using NuGet.Client.Resolution;
using System.Net.Http;
using System.Globalization;
using NuGet.Client.Installation;
using System.Threading;

namespace NuGet.Client
{
    public class V3SourceRepository : SourceRepository, IDisposable
    {
        private DataClient _client;
        private PackageSource _source;
        private Uri _root;
        private string _userAgent;
        private System.Net.Http.HttpClient _http;

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
        public V3SourceRepository(PackageSource source, string host)
        {
            _source = source;
            _root = new Uri(source.Url);

            // TODO: Get context from current UI activity (PowerShell, Dialog, etc.)
            _userAgent = UserAgentUtil.GetUserAgent("NuGet.Client", host);

            _http = new System.Net.Http.HttpClient(
                new TracingHttpHandler(
                    NuGetTraceSources.V3SourceRepository,
                    new SetUserAgentHandler(
                        _userAgent,
                        new HttpClientHandler())));

            // Check if we should disable the browser file cache
            FileCacheBase cache = new BrowserFileCache();
            if (String.Equals(Environment.GetEnvironmentVariable("NUGET_DISABLE_IE_CACHE"), "true", StringComparison.OrdinalIgnoreCase))
            {
                cache = new NullFileCache();
            }

            cache = new NullFileCache(); // +++ Disable caching for testing

            _client = new DataClient(
                _http,
                cache);
        }

        public DataClient DataClient
        {
            get
            {
                return _client;
            }
        }

        public async override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            // Get the search service URL from the service
            cancellationToken.ThrowIfCancellationRequested();
            var searchService = await GetServiceUri(ServiceUris.SearchQueryService, cancellationToken);
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
                var output = await ProcessSearchResult(cancellationToken, result);
                if (output != null)
                {
                    outputs.Add(output);
                }
            }

            return outputs;
        }

        // Async void because we don't want metric recording to block anything at all
        public override async void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, InstallationTarget target)
        {
            var metricsUrl = await GetServiceUri(ServiceUris.MetricsService, CancellationToken.None);

            if (metricsUrl == null)
            {
                // Nothing to do!
                return;
            }

            // Create the JSON payload
            var payload = new JObject();
            payload.Add("id", packageIdentity.Id);
            payload.Add("version", packageIdentity.Version.ToNormalizedString());
            payload.Add("operation", isUpdate ? "Update" : "Install");
            payload.Add("userAgent", _userAgent);
            payload.Add("targetFrameworks", new JArray(target.GetSupportedFrameworks().Select(fx => VersionUtility.GetShortFrameworkName(fx))));
            if (dependentPackage != null)
            {
                payload.Add("dependentPackage", dependentPackage.Id);
                payload.Add("dependentPackageVersion", dependentPackage.Version.ToNormalizedString());
            }
            target.AddMetricsMetadata(payload);

            // Post the message
            await _http.PostAsync(metricsUrl, new StringContent(payload.ToString()));
        }

        // +++ this would not be needed once the result matches searchResult.
        private async Task<JObject> ProcessSearchResult(CancellationToken cancellationToken, JObject result)
        {
            NuGetTraceSources.V3SourceRepository.Verbose(
                                "resolving_package",
                                "Resolving Package: {0}",
                                result[Properties.SubjectId]);
            cancellationToken.ThrowIfCancellationRequested();

            // Get the registration
            result = (JObject)(await _client.Ensure(result, ResultItemRequiredProperties));

            var searchResult = new JObject();
            searchResult["id"] = result["id"];
            searchResult[Properties.LatestVersion] = result[Properties.Version];
            searchResult[Properties.Versions] = result[Properties.Versions];
            searchResult[Properties.Summary] = result[Properties.Summary];
            searchResult[Properties.Description] = result[Properties.Description];
            searchResult[Properties.IconUrl] = result[Properties.IconUrl];

            return searchResult;
        }

        public override async Task<JObject> GetPackageMetadata(string id, NuGetVersion version)
        {
            var data = await GetPackageMetadataById(id);
            return data.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(
                p["version"].ToString(),
                version.ToNormalizedString()));
        }

        public override async Task<IEnumerable<JObject>> GetPackageMetadataById(string packageId)
        {
            // Get the base URL
            var baseUrl = await GetServiceUri(ServiceUris.RegistrationsBaseUrl, CancellationToken.None);
            if (String.IsNullOrEmpty(baseUrl))
            {
                throw new NuGetProtocolException(Strings.Protocol_MissingRegistrationBase);
            }

            // Construct the URL
            var packageUrl = baseUrl.TrimEnd('/') + "/" + packageId.ToLowerInvariant() + "/index.json";

            // Resolve the catalog root
            var catalogPackage = await _client.Ensure(
                new Uri(packageUrl), 
                CatalogRequiredProperties);
            if (catalogPackage["HttpStatusCode"] != null)
            {
                // Got an error response from the data client, so just return an empty array
                return Enumerable.Empty<JObject>();
            }
            // Descend through the items to find all the versions
            var versions = await Descend((JArray)catalogPackage["items"]);

            // Return the catalogEntry values
            return versions.Select(o =>
            {
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
                    var resolved = await _client.Ensure(
                        item, 
                        new[] { new Uri("http://schema.nuget.org/schema#items") });
                    Debug.Assert(resolved != null, "DataClient returned null from Ensure :(");
                    lists.Add(await Descend((JArray)resolved["items"]));
                }
                else if (Equals(type, "Package"))
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

        private async Task<string> GetServiceUri(Uri type, CancellationToken cancellationToken)
        {
            // Read the root document (usually out of the cache :))
            JObject doc;
            var sourceUrl = new Uri(_source.Url);
            if (sourceUrl.IsFile)
            {
                using (var reader = new System.IO.StreamReader(
                    sourceUrl.LocalPath))
                {
                    string json = await reader.ReadToEndAsync();
                    doc = JObject.Parse(json);
                }
            }
            else
            {
                doc = await _client.GetFile(sourceUrl);
            }

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
                              where resourceType != null && Equals(resourceType, type.ToString())
                              select resource)
                             .ToList();
            NuGetTraceSources.V3SourceRepository.Verbose(
                "service_candidates",
                "Found {0} candidates for {1} service: [{2}]",
                candidates.Count,
                type,
                String.Join(", ", candidates.Select(c => c.Value<string>("@id"))));

            var selected = candidates.FirstOrDefault();

            if (selected != null)
            {
                NuGetTraceSources.V3SourceRepository.Info(
                    "getserviceuri",
                    "Found {0} service at {1}",
                    selected["@type"][0],
                    selected["@id"]);
                return selected.Value<string>("@id");
            }
            else
            {
                NuGetTraceSources.V3SourceRepository.Error(
                    "getserviceuri_failed",
                    "Unable to find compatible {0} service on {1}",
                    type,
                    _root);
                return null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _http.Dispose();
                    _client.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}

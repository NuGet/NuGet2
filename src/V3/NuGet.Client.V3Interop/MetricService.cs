using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V3Shim
{
    /// <summary>
    /// v3 metric service for reporting downloads
    /// </summary>
    public class MetricService
    {
        private readonly Uri _metricServiceBaseUrl;

        private const string IdKey = "id";
        private const string VersionKey = "version";
        private const string IPAddressKey = "ipAddress";
        private const string UserAgentKey = "userAgent";
        private const string OperationKey = "operation";
        private const string ParentPackageIdKey = "dependencyRootId";
        private const string ParentPackageVersionKey = "dependencyRootVersion";
        private const string ProjectGuidsKey = "projectGuids";
        private const string HTTPPost = "POST";
        private const string MetricsDownloadEventMethod = "/DownloadEvent";
        private const string ContentTypeJson = "application/json";
        private const string UserAgent = "User-Agent";
        private const string ProjectGuids = "NuGet-ProjectGuids";
        private const string DownloadUrl = "NuGet-DownloadUrl";
        private const string LocalCacheUsed = "NuGet-LocalCacheUsed";

        public MetricService(Uri serviceBaseAddress)
        {
            _metricServiceBaseUrl = serviceBaseAddress;
        }

        /// <summary>
        /// Parse a WebRequest for metric service headers and report them if they exist.
        /// </summary>
        public async Task ProcessRequest(WebRequest request)
        {
            if (request != null)
            {
                try
                {
                    string operation = GetHeaderValue(request, RepositoryOperationNames.OperationHeaderName);

                    if (!String.IsNullOrEmpty(operation) && operation.StartsWith(RepositoryOperationNames.Install, StringComparison.OrdinalIgnoreCase))
                    {
                        string agent = String.Format(CultureInfo.InvariantCulture, "{0}, {1})", GetHeaderValue(request, UserAgent).TrimEnd(')'), ShimConstants.ShimUserAgent);

                        string id = GetHeaderValue(request, RepositoryOperationNames.PackageId);
                        string version = GetHeaderValue(request, RepositoryOperationNames.PackageVersion);
                        string projectGuids = GetHeaderValue(request, ProjectGuids);
                        string depId = GetHeaderValue(request, RepositoryOperationNames.DependentPackageHeaderName);
                        string depVer = GetHeaderValue(request, RepositoryOperationNames.DependentPackageVersionHeaderName);

                        // id and version must exist
                        if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(version))
                        {
                            var jObject = new JObject();
                            jObject.Add(IdKey, id);
                            jObject.Add(VersionKey, version);
                            jObject.Add(DownloadUrl, request.RequestUri.AbsoluteUri);
                            jObject.Add(LocalCacheUsed, "false");
                            if (!String.IsNullOrEmpty(agent)) jObject.Add(UserAgentKey, agent);
                            if (!String.IsNullOrEmpty(operation)) jObject.Add(OperationKey, operation);
                            if (!String.IsNullOrEmpty(projectGuids)) jObject.Add(ProjectGuidsKey, projectGuids);
                            if (!String.IsNullOrEmpty(depId)) jObject.Add(ParentPackageIdKey, depId);
                            if (!String.IsNullOrEmpty(depVer)) jObject.Add(ParentPackageVersionKey, depVer);

                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                await httpClient.PostAsync(new Uri(_metricServiceBaseUrl.AbsoluteUri.TrimEnd('/') + MetricsDownloadEventMethod), new StringContent(jObject.ToString(), Encoding.UTF8, ContentTypeJson));
                            }
                        }
                        else
                        {
                            Debug.Fail("invalid id and version");
                        }
                    }
                }
                catch
                {
                    // ignore failures
                    Debug.Fail("failed to report download metrics");
                }
            }
        }

        private static string GetHeaderValue(WebRequest request, string key)
        {
            var values = request.Headers.GetValues(key);

            return values == null || values.Length < 1 ? null : values[0];
        }

    }
}

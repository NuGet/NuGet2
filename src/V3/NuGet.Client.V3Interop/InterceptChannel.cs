using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGet.Client.V3Shim
{
    internal class InterceptChannel
    {
        private readonly string _resolverBaseAddress;
        private readonly string _searchAddress;
        private readonly string _passThroughAddress;
        private readonly string _listAvailableLatestStableIndex;
        private readonly string _listAvailableAllIndex;
        private readonly string _listAvailableLatestPrereleaseIndex;
        private readonly MetricService _metricService;
        private readonly string _source;
        private readonly IShimCache _cache;

        internal InterceptChannel(string source, JObject interceptBlob, IShimCache cache)
        {
            _resolverBaseAddress = interceptBlob["resolverBaseAddress"].ToString().TrimEnd('/');
            _searchAddress = interceptBlob["searchAddress"].ToString().TrimEnd('/');
            _passThroughAddress = interceptBlob["passThroughAddress"].ToString().TrimEnd('/');
            _listAvailableLatestStableIndex = interceptBlob["isLatestStable"].ToString();
            _listAvailableAllIndex = interceptBlob["allVersions"].ToString();
            _listAvailableLatestPrereleaseIndex = interceptBlob["isLatest"].ToString();

            if (interceptBlob["metricAddress"] != null)
            {
                _metricService = new MetricService(new Uri(interceptBlob["metricAddress"].ToString()));
            }
            else
            {
                // TODO: Remove this once it has been added to intercept.json
                _metricService = new MetricService(new Uri("http://api-metrics.nuget.org"));
            }

            _source = source.TrimEnd('/');
            _cache = cache;
        }

        public static bool TryCreate(string source, IShimCache cache, out InterceptChannel channel)
        {
            // create the interceptor from the intercept.json file. exper
            string interceptUrl = String.Format(CultureInfo.InvariantCulture, "{0}/intercept.json", source);

            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                var reqTask = client.GetAsync(interceptUrl);
                reqTask.Wait();
                HttpResponseMessage response = reqTask.Result;

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var resTask = response.Content.ReadAsStringAsync();
                    resTask.Wait();
                    JObject obj = JObject.Parse(resTask.Result);

                    channel = new InterceptChannel(source, obj, cache);
                    return true;
                }

                channel = null;
                return false;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public async Task Root(InterceptCallContext context, string feedName = null)
        {
            V3InteropTraceSources.Channel.Verbose("root", feedName ?? String.Empty);

            if (feedName == null)
            {
                Stream stream = GetResourceStream("xml.Root.xml");
                XElement xml = XElement.Load(stream);
                await context.WriteResponse(xml);
            }
            else
            {
                Stream stream = GetResourceStream("xml.FeedRoot.xml");
                string s = (new StreamReader(stream)).ReadToEnd();
                string t = string.Format(s, feedName);
                XElement xml = XElement.Load(new StringReader(t), LoadOptions.SetBaseUri);
                await context.WriteResponse(xml);
            }
        }

        public async Task ReportMetrics(WebRequest request)
        {
            // metrics
            if (_metricService != null)
            {
                await _metricService.ProcessRequest(request);
            }
        }

        public async Task DownloadPackage(InterceptCallContext context, string id, string version)
        {
            JToken package = await GetPackageCore(context, id, version);

            if (package == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "unable to find version {0} of package {1}", version, id));
            }

            string address = package["nupkgUrl"].ToString();


            V3InteropTraceSources.Channel.Info("downloadingpackage", "Downloading {0}", address);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            HttpResponseMessage response = await client.GetAsync(address);
            byte[] data = await response.Content.ReadAsByteArrayAsync();

            timer.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                V3InteropTraceSources.Channel.Info("downloadedpackage", "Downloaded {0} in {1}ms", address, timer.ElapsedMilliseconds);
            }
            else
            {
                V3InteropTraceSources.Channel.Error("downloadedpackage_error", "Error downloading {0} in {1}ms (status {2})", address, timer.ElapsedMilliseconds, response.StatusCode);
            }

            await context.WriteResponse(data, "application/zip");
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public async Task Metadata(InterceptCallContext context, string feed = null)
        {
            V3InteropTraceSources.Channel.Verbose("metadata", feed ?? String.Empty);

            Stream stream = GetResourceStream(feed == null ? "xml.Metadata.xml" : "xml.FeedMetadata.xml");
            XElement xml = XElement.Load(stream);
            await context.WriteResponse(xml);
        }

        public async Task SearchCount(InterceptCallContext context, string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, int skip, int take, string feedName, string sortBy)
        {
            V3InteropTraceSources.Channel.Verbose("searchcount", searchTerm);
            
            JObject obj = await FetchJson(context, MakeSearchAddress(searchTerm, isLatestVersion, targetFramework, includePrerelease, skip, take, feedName, sortBy));

            string count = obj != null ? count = obj["totalHits"].ToString() : "0";

            await context.WriteResponse(count);
        }

        public async Task Search(InterceptCallContext context, string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, int skip, int take, string feedName, string sortBy)
        {
            V3InteropTraceSources.Channel.Verbose("search", "{0} ({1},{2})", searchTerm, skip, take);
            
            JObject obj = await FetchJson(context, MakeSearchAddress(searchTerm, isLatestVersion, targetFramework, includePrerelease, skip, take, feedName, sortBy));

            IEnumerable<JToken> data = (obj != null) ? data = obj["data"] : Enumerable.Empty<JToken>();

            XElement feed = InterceptFormatting.MakeFeedFromSearch(_source, _passThroughAddress, "Packages", data, "");
            await context.WriteResponse(feed);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "feedName")]
        public async Task GetPackage(InterceptCallContext context, string id, string version, string feedName)
        {
            V3InteropTraceSources.Channel.Verbose("getpackage", "{0} {1}", id, version);
            
            var desiredPackage = await GetPackageCore(context, id, version);

            if (desiredPackage == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "unable to find version {0} of package {1}", version, id));
            }

            XElement feed = InterceptFormatting.MakeFeed(_passThroughAddress, "Packages", new List<JToken> { desiredPackage }, id);
            await context.WriteResponse(feed);
        }

        private async Task<JToken> GetPackageCore(InterceptCallContext context, string id, string version)
        {
            JToken desiredPackage = null;
            JObject resolverBlob = await FetchJson(context, MakeResolverAddress(id));


            if (resolverBlob != null)
            {
                NuGetVersion desiredVersion = NuGetVersion.Parse(version);

                foreach (JToken package in resolverBlob["packages"])
                {
                    NuGetVersion currentVersion = NuGetVersion.Parse(package["version"].ToString());
                    if (currentVersion == desiredVersion)
                    {
                        desiredPackage = package;
                        break;
                    }
                }
            }

            return desiredPackage;
        }


        public async Task GetLatestVersionPackage(InterceptCallContext context, string id, bool includePrerelease)
        {
            V3InteropTraceSources.Channel.Verbose("getlatestversionpackage", "{0} Pre={1}", id, includePrerelease);
            
            JObject resolverBlob = await FetchJson(context, MakeResolverAddress(id));

            if (resolverBlob == null)
            {
                throw new InvalidOperationException(string.Format("package {0} not found", id));
            }

            JToken latest = ExtractLatestVersion(resolverBlob, includePrerelease);

            if (latest == null)
            {
                throw new InvalidOperationException(string.Format("package {0} not found", id));
            }

            XElement feed = InterceptFormatting.MakeFeed(_passThroughAddress, "Packages", new List<JToken> { latest }, id);
            await context.WriteResponse(feed);
        }

        public async Task GetAllPackageVersions(InterceptCallContext context, string id)
        {
            V3InteropTraceSources.Channel.Verbose("getallpackageversions", id);

            var ids = id.Split(new string[] { " or " }, StringSplitOptions.RemoveEmptyEntries);

            List<JToken> packages = new List<JToken>();

            foreach (var s in ids)
            {
                string curId = s.Trim('\'');

                if (curId.StartsWith("tolower(id) eq '"))
                {
                    curId = curId.Split('\'')[1];
                }

                // TODO: run in parallel
                JObject resolverBlob = await FetchJson(context, MakeResolverAddress(curId));

                if (resolverBlob == null)
                {
                    throw new InvalidOperationException(string.Format("package {0} not found", curId));
                }

                foreach (var p in resolverBlob["packages"])
                {
                    NuGetVersion version = NuGetVersion.Parse(p["version"].ToString());

                    // all versions are returned, filter to only stable if needed
                    if (context.Args.IncludePrerelease || !version.IsPrerelease)
                    {
                        p["id"] = resolverBlob["id"];

                        packages.Add(p);
                    }
                }
            }

            var data = packages.OrderBy(p => p["id"].ToString()).ThenByDescending(p => NuGetVersion.Parse(p["version"].ToString()), VersionComparer.VersionRelease);

            XElement feed = InterceptFormatting.MakeFeed(_passThroughAddress, "Packages", data, data.Select(p => p["id"].ToString()).ToArray());
            await context.WriteResponse(feed);
        }

        public async Task GetListOfPackageVersions(InterceptCallContext context, string id)
        {
            V3InteropTraceSources.Channel.Verbose("GetListOfPackageVersions", id);
            
            JObject resolverBlob = await FetchJson(context, MakeResolverAddress(id));

            JArray array = new JArray();

            // the package may not exist, in that case return an empty array
            if (resolverBlob != null)
            {
                List<NuGetVersion> versions = new List<NuGetVersion>();
                foreach (JToken package in resolverBlob["packages"])
                {
                    NuGetVersion version = NuGetVersion.Parse(package["version"].ToString());

                    // all versions are returned, filter to only stable if needed
                    if (context.Args.IncludePrerelease || !version.IsPrerelease)
                    {
                        versions.Add(version);
                    }
                }

                versions.Sort();

                foreach (NuGetVersion version in versions)
                {
                    array.Add(version.ToString());
                }
            }

            await context.WriteResponse(array);
        }

        public async Task GetListOfPackages(InterceptCallContext context)
        {
            V3InteropTraceSources.Channel.Verbose("GetListOfPackages", String.Empty);
            
            var index = await FetchJson(context, context.Args.IncludePrerelease ? new Uri(_listAvailableLatestPrereleaseIndex) : new Uri(_listAvailableLatestStableIndex));
            var data = GetListAvailableDataStart(context, index);

            // apply startswith if needed
            if (context.Args.PartialId != null)
            {
                data = data.Where(e => e["id"].ToString().StartsWith(context.Args.PartialId, StringComparison.OrdinalIgnoreCase));

                data = data.TakeWhile(e => e["id"].ToString().StartsWith(context.Args.PartialId, StringComparison.OrdinalIgnoreCase));
            }

            // take only 30
            var ids = data.Take(30).Select(p => p["id"].ToString()).ToList();

            ids.Sort(StringComparer.InvariantCultureIgnoreCase);

            JArray array = new JArray();
            foreach (var id in ids)
            {
                array.Add(id);
            }

            await context.WriteResponse(array);
        }

        public async Task ListAvailable(InterceptCallContext context)
        {
            string indexUrl = _listAvailableLatestStableIndex;

            if (!context.Args.IsLatestVersion)
            {
                indexUrl = _listAvailableAllIndex;
            }
            else if (context.Args.IncludePrerelease)
            {
                indexUrl = _listAvailableLatestPrereleaseIndex;
            }

            var index = await FetchJson(context, new Uri(indexUrl));

            var data = GetListAvailableData(context, index);

            // all versions with no pre
            if (!context.Args.IsLatestVersion && !context.Args.IncludePrerelease)
            {
                data = data.Where(p => (new NuGetVersion(p["version"].ToString())).IsPrerelease == false);
            }

            string nextUrl = null;

            // Convert to a list after calling Take to avoid enumerating the list multiple times.

            if (context.Args.Top.HasValue && context.Args.Top.Value > 0)
            {
                data = data.Take(context.Args.Top.Value).ToList();
            }
            else
            {
                data = data.Take(30).ToList();

                if (data.Count() >= 30)
                {
                    var last = data.LastOrDefault();

                    if (last != null)
                    {
                        string argsWithoutSkipToken = String.Join("&", context.Args.Arguments.Where(a => a.Key.ToLowerInvariant() != "$skiptoken")
                            .Select(a => String.Format(CultureInfo.InvariantCulture, "{0}={1}", a.Key, a.Value)));

                        nextUrl = String.Format(CultureInfo.InvariantCulture, "{0}?{1}&$skiptoken='{2}','{2}','{3}'",
                                                context.RequestUri.AbsoluteUri.Split('?')[0],
                                                argsWithoutSkipToken,
                                                last["id"],
                                                last["version"]);
                    }
                }
            }

            XElement feed = InterceptFormatting.MakeFeed(_passThroughAddress, "Packages", data, data.Select(e => e["id"].ToString()).ToArray(), nextUrl);
            await context.WriteResponse(feed);
        }

        public IEnumerable<JToken> GetListAvailableData(InterceptCallContext context, JObject index)
        {
            var data = GetListAvailableFastForwardSkipToken(context, index);

            // apply startswith if needed
            if (context.Args.FilterStartsWithId != null)
            {
                data = data.Where(e => e["id"].ToString().StartsWith(context.Args.FilterStartsWithId, StringComparison.OrdinalIgnoreCase));

                data = data.TakeWhile(e => e["id"].ToString().StartsWith(context.Args.FilterStartsWithId, StringComparison.OrdinalIgnoreCase));
            }

            return data;
        }

        public IEnumerable<JToken> GetListAvailableFastForwardSkipToken(InterceptCallContext context, JObject index)
        {
            var data = GetListAvailableDataStart(context, index);

            if (context.Args.SkipToken != null)
            {
                var skipToken = ParseSkipToken(context.Args.SkipToken);
                var skipTokenId = skipToken.Item1;
                var skipTokenVer = new NuGetVersion(skipToken.Item2);

                data = data.Where(e => skipTokenId == null || StringComparer.InvariantCultureIgnoreCase.Compare(e["id"].ToString(), skipTokenId) > 0
                    || (StringComparer.InvariantCultureIgnoreCase.Equals(e["id"].ToString(), skipTokenId) &&
                        (skipTokenVer == null || VersionComparer.VersionRelease.Compare(new NuGetVersion(e["version"].ToString()), skipTokenVer) > 0)));
            }

            return data;
        }

        public IEnumerable<JToken> GetListAvailableDataStart(InterceptCallContext context, JObject index)
        {
            string skipTo = null;

            if (context.Args.SkipToken != null)
            {
                var skipToken = ParseSkipToken(context.Args.SkipToken);
                skipTo = skipToken.Item1;
            }
            else if (context.Args.FilterStartsWithId != null)
            {
                skipTo = context.Args.FilterStartsWithId;
            }
            if (context.Args.PartialId != null) // intellisense
            {
                skipTo = context.Args.PartialId;
            }

            var segments = GetListAvailableSegmentsIncludingAndAfter(context, index, skipTo);

            foreach(var segUrl in segments)
            {
                var dataTask = FetchJson(context, new Uri(segUrl));
                dataTask.Wait();
                var data = dataTask.Result;

                var orderedData = data["entry"].OrderBy(e => e["id"].ToString(), StringComparer.InvariantCultureIgnoreCase)
                    .ThenBy(e => new NuGetVersion(e["version"].ToString()), VersionComparer.VersionRelease);

                foreach (var entry in orderedData)
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        ///  Skip to the first needed segment, and include all following segments.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context")]
        public static Queue<string> GetListAvailableSegmentsIncludingAndAfter(InterceptCallContext context, JObject index, string startsWith=null)
        {
            var segs = index["entry"].ToArray();

            Queue<string> needed = new Queue<string>();

            for (int i=0; i < segs.Length; i++)
            {
                // once we add the first one, take everythign after
                if (needed.Count > 0 || String.IsNullOrEmpty(startsWith))
                {
                    needed.Enqueue(segs[i]["url"].ToString());
                }
                else
                {
                    var seg = segs[i];

                    string lowest = seg["lowest"].ToString();

                    // advance until we go too far
                    if (needed.Count < 1 && StringComparer.InvariantCultureIgnoreCase.Compare(lowest, startsWith) >= 0)
                    {
                        if (i > 0)
                        {
                            // get the previous one
                            needed.Enqueue(segs[i - 1]["url"].ToString());
                        }

                        // add the current one
                        needed.Enqueue(segs[i]["url"].ToString());
                    }
                }
            }

            return needed;
        }

        public async Task GetUpdates(InterceptCallContext context, string[] packageIds, string[] versions, string[] versionConstraints, string[] targetFrameworks, bool includePrerelease, bool includeAllVersions, bool count = false)
        {
            V3InteropTraceSources.Channel.Info(count ? "getupdates" : "getupdatescount", String.Join(", ", packageIds));

            var packages = await GetUpdatesCore(context, packageIds, versions, versionConstraints, targetFrameworks, includePrerelease, includeAllVersions);

            if (count)
            {
                await context.WriteResponse(packages.Count);
            }
            else
            {
                XElement feed = InterceptFormatting.MakeFeed(_passThroughAddress, "GetUpdates", packages, packages.Select(p => p["id"].ToString()).ToArray());
                await context.WriteResponse(feed);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "includeAllVersions"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "targetFrameworks"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "versions")]
        public async Task<List<JObject>> GetUpdatesCore(InterceptCallContext context, string[] packageIds, string[] versions, string[] versionConstraints, string[] targetFrameworks, bool includePrerelease, bool includeAllVersions)
        {
            List<JObject> packages = new List<JObject>();

            for (int i = 0; i < packageIds.Length; i++)
            {
                VersionRange range = null;

                if (i < versionConstraints.Length && !String.IsNullOrEmpty(versionConstraints[i]))
                {
                    VersionRange.TryParse(versionConstraints[i], out range);
                }

                JObject resolverBlob = await FetchJson(context, MakeResolverAddress(packageIds[i]));

                // TODO: handle this error
                if (resolverBlob != null)
                {
                    // this can be null if all packages are prerelease
                    JObject latest = ExtractLatestVersion(resolverBlob, includePrerelease, range) as JObject;
                    if (latest != null)
                    {
                        // add the id if it isn't there
                        if (latest["id"] == null)
                        {
                            latest.Add("id", JToken.Parse("'" + packageIds[i] + "'"));
                        }

                        if (i < versions.Length && !String.IsNullOrEmpty(versions[i]))
                        {
                            NuGetVersion latestVersion = NuGetVersion.Parse(latest["version"].ToString());
                            NuGetVersion currentVersion = NuGetVersion.Parse(versions[i]);

                            // only add the package if it is not the latest version
                            if (VersionComparer.VersionRelease.Compare(latestVersion, currentVersion) > 0)
                            {
                                packages.Add(latest);
                            }
                        }
                        else
                        {
                            packages.Add(latest);
                        }
                    }
                }
            }

            return packages;
        }

        private static JToken ExtractLatestVersion(JObject resolverBlob, bool includePrerelease, VersionRange range = null)
        {
            // sort by version
            JToken candidateLatest = resolverBlob["packages"]
                .Where(p => includePrerelease || (new NuGetVersion(p["version"].ToString()).IsPrerelease == false))
                .Where(p => range == null || range.Satisfies(new NuGetVersion(p["version"].ToString())))
                .OrderByDescending(p => new NuGetVersion(p["version"].ToString()), VersionComparer.VersionRelease)
                .FirstOrDefault();

            return candidateLatest;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        private Uri MakeResolverAddress(string id)
        {
            id = id.ToLowerInvariant();
            Uri resolverBlobAddress = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/{1}.json", _resolverBaseAddress, id));
            return resolverBlobAddress;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "isLatestVersion")]
        private Uri MakeSearchAddress(string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, int skip, int take, string feedName, string sortBy)
        {
            string feedArg = feedName == null ? string.Empty : string.Format(CultureInfo.InvariantCulture, "&feed={0}", feedName);

            Uri searchAddress = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}?q={1}&luceneQuery=false&targetFramework={2}&prerelease={3}&skip={4}&sortBy={5}&take={6}{7}",
                _searchAddress, searchTerm, targetFramework, includePrerelease, skip, sortBy, take, feedArg));
            return searchAddress;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context")]
        private async Task<JObject> FetchJson(InterceptCallContext context, Uri address)
        {
            JObject fromCache = null;
            if (_cache.TryGet(address, out fromCache))
            {
                V3InteropTraceSources.Channel.Verbose("cachehit", "Cache HIT : {0}", address);
                return fromCache;
            }

            V3InteropTraceSources.Channel.Verbose("cachemiss", "Cache MISS: {0}", address);
            
            Stopwatch timer = new Stopwatch();
            timer.Start();

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            HttpResponseMessage response = await client.GetAsync(address);

            timer.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                V3InteropTraceSources.Channel.Verbose("jsonresp", "Retrieved {0} in {1}ms.", address, timer.ElapsedMilliseconds);
                string json = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(json);

                _cache.AddOrUpdate(address, obj);

                return obj;
            }
            else
            {
                // expected in some cases
                V3InteropTraceSources.Channel.Verbose("jsonresp", "{2} error retrieving {0} in {1}ms.", address, timer.ElapsedMilliseconds, (int)response.StatusCode);
                return null;
            }
        }

        public static Stream GetResourceStream(string resName)
        {
            var assem = Assembly.GetExecutingAssembly();

            // TODO: replace this
            var resource = assem.GetManifestResourceNames().Where(s => s.IndexOf(resName, StringComparison.OrdinalIgnoreCase) > -1).FirstOrDefault();

            var stream = assem.GetManifestResourceStream(resource);
            return stream;
        }

        private static Tuple<string, string> ParseSkipToken(string skipToken)
        {
            string prevId = string.Empty;
            string prevVer = string.Empty;
            if (!String.IsNullOrEmpty(skipToken))
            {
                var parts = skipToken.Split(',');

                if (parts.Length == 3)
                {
                    prevId = parts[0].Trim(new char[] { ' ', '\'' });
                    prevVer = parts[2].Trim(new char[] { ' ', '\'' });

                    return new Tuple<string, string>(prevId, prevVer);
                }
            }

            throw new InvalidOperationException("Invalid skip token");
        }
    }
}

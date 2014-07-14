using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGet.ShimV3
{
    internal class InterceptChannel
    {
        private readonly string _resolverBaseAddress;
        private readonly string _searchAddress;
        private readonly string _passThroughAddress;
        private readonly string _listAvailableLatestStableIndex;
        private readonly string _listAvailableAllIndex;
        private readonly string _listAvailableLatestPrereleaseIndex;
        private readonly IShimCache _cache;

        internal InterceptChannel(JObject interceptBlob, IShimCache cache)
        {
            _resolverBaseAddress = interceptBlob["resolverBaseAddress"].ToString().TrimEnd('/');
            _searchAddress = interceptBlob["searchAddress"].ToString().TrimEnd('/');
            _passThroughAddress = interceptBlob["passThroughAddress"].ToString().TrimEnd('/');
            _listAvailableLatestStableIndex = interceptBlob["isLatestStable"].ToString();
            _listAvailableAllIndex = interceptBlob["allVersions"].ToString();
            _listAvailableLatestPrereleaseIndex = interceptBlob["isLatest"].ToString();
            _cache = cache;
        }

        public static bool TryCreate(string source, IShimCache cache, out InterceptChannel channel)
        {
            // create the interceptor from the intercept.json file. exper
            string interceptUrl = String.Format(CultureInfo.InvariantCulture, "{0}/intercept.json", source);

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            var reqTask = client.GetAsync(interceptUrl);
            reqTask.Wait();
            HttpResponseMessage response = reqTask.Result;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var resTask = response.Content.ReadAsStringAsync();
                resTask.Wait();
                JObject obj = JObject.Parse(resTask.Result);

                channel = new InterceptChannel(obj, cache);
                return true;
            }

            channel = null;
            return false;
        }

        public async Task Root(InterceptCallContext context, string feedName = null)
        {
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] Root: {0}", feedName ?? string.Empty), ConsoleColor.Magenta);

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

        public async Task Metadata(InterceptCallContext context, string feed = null)
        {
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] Metadata: {0}", feed ?? string.Empty), ConsoleColor.Magenta);

            Stream stream = GetResourceStream(feed == null ? "xml.Metadata.xml" : "xml.FeedMetadata.xml");
            XElement xml = XElement.Load(stream);
            await context.WriteResponse(xml);
        }

        public async Task Count(InterceptCallContext context, string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, string feedName)
        {
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] Count: {0}", searchTerm), ConsoleColor.Magenta);

            JObject obj = await FetchJson(context, MakeCountAddress(searchTerm, isLatestVersion, targetFramework, includePrerelease, feedName));

            string count = obj != null ? count = obj["totalHits"].ToString() : "0";

            await context.WriteResponse(count);
        }

        public async Task Search(InterceptCallContext context, string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, int skip, int take, string feedName)
        {
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] Search: {0} ({1},{2})", searchTerm, skip, take), ConsoleColor.Magenta);

            JObject obj = await FetchJson(context, MakeSearchAddress(searchTerm, isLatestVersion, targetFramework, includePrerelease, skip, take, feedName));

            IEnumerable<JToken> data = (obj != null) ? data = obj["data"] : Enumerable.Empty<JToken>();

            XElement feed = InterceptFormatting.MakeFeedFromSearch(_passThroughAddress, "Packages", data, "");
            await context.WriteResponse(feed);
        }

        public async Task GetPackage(InterceptCallContext context, string id, string version, string feedName)
        {
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] GetPackage: {0} {1}", id, version), ConsoleColor.Magenta);

            JObject resolverBlob = await FetchJson(context, MakeResolverAddress(id));
            JToken desiredPackage = null;

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

            if (desiredPackage == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "unable to find version {0} of package {1}", version, id));
            }

            XElement feed = InterceptFormatting.MakeFeed(_passThroughAddress, "Packages", new List<JToken> { desiredPackage }, id);
            await context.WriteResponse(feed);
        }

        public async Task GetLatestVersionPackage(InterceptCallContext context, string id, bool includePrerelease)
        {
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] GetLatestVersionPackage: {0} {1}", id, includePrerelease ? "[include prerelease]" : ""), ConsoleColor.Magenta);

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
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] GetAllPackageVersions: {0}", id), ConsoleColor.Magenta);

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
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] GetListOfPackageVersions: {0}", id), ConsoleColor.Magenta);

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
            context.Log("[V3 CALL] GetListOfPackages", ConsoleColor.Magenta);

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
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] GetUpdates{1}: {0}", string.Join("|", packageIds), count ? "Count" : string.Empty), ConsoleColor.Magenta);

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

        public async Task<List<JObject>> GetUpdatesCore(InterceptCallContext context, string[] packageIds, string[] versions, string[] versionConstraints, string[] targetFrameworks, bool includePrerelease, bool includeAllVersions)
        {
            List<JObject> packages = new List<JObject>();

            for (int i = 0; i < packageIds.Length; i++)
            {
                VersionRange range = null;

                if (versionConstraints.Length < i && !String.IsNullOrEmpty(versionConstraints[i]))
                {
                    VersionRange.TryParse(versionConstraints[i], out range);
                }

                JObject resolverBlob = await FetchJson(context, MakeResolverAddress(packageIds[i]));

                // TODO: handle this error
                if (resolverBlob != null)
                {
                    JObject latest = ExtractLatestVersion(resolverBlob, includePrerelease, range) as JObject;
                    if (latest == null)
                    {
                        throw new InvalidOperationException(string.Format("package {0} not found", packageIds[i]));
                    }

                    // add the id if it isn't there
                    if (latest["id"] == null)
                    {
                        latest.Add("id", JToken.Parse("'" + packageIds[i] + "'"));
                    }

                    packages.Add(latest);
                }
            }

            return packages;
        }

        private static JToken ExtractLatestVersion(JObject resolverBlob, bool includePrerelease, VersionRange range = null)
        {
            //  firstly just pick the first one (or the first in range)

            JToken candidateLatest = null;

            if (range == null)
            {
                candidateLatest = resolverBlob["packages"].FirstOrDefault();
            }
            else
            {
                foreach (JToken package in resolverBlob["packages"])
                {
                    NuGetVersion currentVersion = NuGetVersion.Parse(package["version"].ToString());
                    if (range.Satisfies(currentVersion))
                    {
                        candidateLatest = package;
                        break;
                    }
                }
            }

            if (candidateLatest == null)
            {
                return null;
            }

            //  secondly iterate through package to see if we have a later package

            NuGetVersion candidateLatestVersion = NuGetVersion.Parse(candidateLatest["version"].ToString());

            foreach (JToken package in resolverBlob["packages"])
            {
                NuGetVersion currentVersion = NuGetVersion.Parse(package["version"].ToString());

                if (range != null && !range.Satisfies(currentVersion))
                {
                    continue;
                }

                if (includePrerelease)
                {
                    if (currentVersion > candidateLatestVersion)
                    {
                        candidateLatest = package;
                        candidateLatestVersion = currentVersion;
                    }
                }
                else
                {
                    if (!currentVersion.IsPrerelease && currentVersion > candidateLatestVersion)
                    {
                        candidateLatest = package;
                        candidateLatestVersion = currentVersion;
                    }
                }
            }

            if (candidateLatestVersion.IsPrerelease && !includePrerelease)
            {
                return null;
            }

            return candidateLatest;
        }

        private Uri MakeResolverAddress(string id)
        {
            id = id.ToLowerInvariant();
            Uri resolverBlobAddress = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/{1}.json", _resolverBaseAddress, id));
            return resolverBlobAddress;
        }

        private Uri MakeCountAddress(string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, string feedName)
        {
            string feedArg = feedName == null ? string.Empty : string.Format(CultureInfo.InvariantCulture, "&feed={0}", feedName);

            Uri searchAddress = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}?q={1}&targetFramework={2}&includePrerelease={3}&countOnly=true{4}",
                _searchAddress, searchTerm, targetFramework, includePrerelease, feedArg));

            return searchAddress;
        }

        private Uri MakeSearchAddress(string searchTerm, bool isLatestVersion, string targetFramework, bool includePrerelease, int skip, int take, string feedName)
        {
            string feedArg = feedName == null ? string.Empty : string.Format(CultureInfo.InvariantCulture, "&feed={0}", feedName);

            Uri searchAddress = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}?q={1}&targetFramework={2}&includePrerelease={3}&skip={4}&take={5}{6}",
                _searchAddress, searchTerm, targetFramework, includePrerelease, skip, take, feedArg));
            return searchAddress;
        }

        private async Task<JObject> FetchJson(InterceptCallContext context, Uri address)
        {
            string url = address.ToString().ToLowerInvariant();

            JObject fromCache = null;
            if (_cache.TryGet(address, out fromCache))
            {
                context.Log(String.Format(CultureInfo.InvariantCulture, "[V3 CACHE] {0}", address.ToString()), ConsoleColor.DarkCyan);
                return fromCache;
            }

            context.Log(String.Format(CultureInfo.InvariantCulture, "[V3 REQ] {0}", address.ToString()), ConsoleColor.Cyan);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            HttpResponseMessage response = await client.GetAsync(address);

            timer.Stop();

            context.Log(String.Format(CultureInfo.InvariantCulture, "[V3 RES] (status:{0}) (time:{1}ms) {2}", response.StatusCode, timer.ElapsedMilliseconds, address.ToString()),
                response.StatusCode == System.Net.HttpStatusCode.OK ? ConsoleColor.Cyan : ConsoleColor.Red);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string json = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(json);

                _cache.AddOrUpdate(address, obj);

                return obj;
            }
            else
            {
                // expected in some cases
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

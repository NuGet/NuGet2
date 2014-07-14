using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace NuGet.ShimV3
{
    internal class InterceptDispatcher
    {
        private readonly Tuple<string, Func<InterceptCallContext, Task>>[] _funcs;
        private readonly Tuple<string, Func<InterceptCallContext, Task>>[] _feedFuncs;
        private InterceptChannel _channel;
        private readonly string _source;
        private bool? _initialized;
        private readonly IShimCache _cache;

        /// <summary>
        /// Creates an uninitialized InterceptDispatcher.
        /// </summary>
        /// <param name="source">Source url from VS package sources.</param>
        /// <param name="cache">Cache for storing json blobs.</param>
        public InterceptDispatcher(string source, IShimCache cache)
        {
            _funcs = new Tuple<string, Func<InterceptCallContext, Task>>[]
            {
                new Tuple<string, Func<InterceptCallContext, Task>>("Search()/$count", Count),
                new Tuple<string, Func<InterceptCallContext, Task>>("Search", Search),
                new Tuple<string, Func<InterceptCallContext, Task>>("FindPackagesById", FindPackagesById),
                new Tuple<string, Func<InterceptCallContext, Task>>("GetUpdates", GetUpdates),
                new Tuple<string, Func<InterceptCallContext, Task>>("Packages", Packages),
                new Tuple<string, Func<InterceptCallContext, Task>>("package-ids", PackageIds),
                new Tuple<string, Func<InterceptCallContext, Task>>("package-versions", PackageVersions),
                new Tuple<string, Func<InterceptCallContext, Task>>("$metadata", Metadata)
            };

            _feedFuncs = new Tuple<string, Func<InterceptCallContext, Task>>[]
            {
                new Tuple<string, Func<InterceptCallContext, Task>>("Search()/$count", Feed_Count),
                new Tuple<string, Func<InterceptCallContext, Task>>("Search", Feed_Search),
                new Tuple<string, Func<InterceptCallContext, Task>>("FindPackagesById", Feed_FindPackagesById),
                new Tuple<string, Func<InterceptCallContext, Task>>("Packages", Feed_Packages),
                new Tuple<string, Func<InterceptCallContext, Task>>("$metadata", Feed_Metadata)
            };

            _source = source.Trim('/');
            _initialized = null;
            _cache = cache;
        }

        /// <summary>
        /// Attempt to enable the channel by fetching intercept.json
        /// </summary>
        public bool TryInit()
        {
            InterceptChannel channel = null;
            if (InterceptChannel.TryCreate(_source, _cache, out channel))
            {
                _channel = channel;
                _initialized = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Source url from the VS package source provider.
        /// </summary>
        public string Source
        {
            get
            {
                return _source;
            }
        }

        /// <summary>
        /// True - V3 source
        /// False - V2 source
        /// Null - No requests made yet
        /// </summary>
        public bool? Initialized
        {
            get
            {
                return _initialized;
            }

            internal set
            {
                _initialized = value;
            }
        }

        /// <summary>
        /// Gathers the response data from the V3 source for the context uri.
        /// </summary>
        public async Task Invoke(InterceptCallContext context)
        {
            try
            {
                if (_initialized != true)
                {
                    throw new InvalidOperationException("Requires Init");
                }

                string unescapedAbsolutePath = Uri.UnescapeDataString(context.RequestUri.AbsolutePath);

                string path = unescapedAbsolutePath;

                // v2 is still in the path for get-package
                if (unescapedAbsolutePath.IndexOf(ShimConstants.V2UrlPath) > -1)
                {
                    path = unescapedAbsolutePath.Remove(0, ShimConstants.V2UrlPath.Length);
                }
                else if (unescapedAbsolutePath.IndexOf(ShimConstants.V3UrlPath) > -1)
                {
                    path = unescapedAbsolutePath.Remove(0, ShimConstants.V3UrlPath.Length);
                }

                foreach (var func in _funcs)
                {
                    if (path == string.Empty)
                    {
                        await Root(context);
                        return;
                    }
                    else if (path.StartsWith(func.Item1))
                    {
                        await func.Item2(context);
                        return;
                    }
                }

                //  url was not recognized - perhaps this is a feed

                int index1 = path.IndexOf('/', 0) + 1;
                if (index1 < path.Length)
                {
                    int index2 = path.IndexOf('/', index1);
                    if (index2 < path.Length)
                    {
                        path = path.Remove(0, index2 + 1);
                    }
                }

                foreach (var func in _feedFuncs)
                {
                    if (path == string.Empty)
                    {
                        await _channel.Root(context);

                        //await Feed_Root(context);
                        return;
                    }
                    if (path.StartsWith(func.Item1))
                    {
                        await func.Item2(context);
                        return;
                    }
                }

                // unknown process
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                context.Log(String.Format(CultureInfo.InvariantCulture, "[V3 ERR] (exception:{0}) {1}", ex.GetType().ToString(), context.RequestUri.AbsoluteUri), ConsoleColor.Red);
                throw;
            }
        }

        private async Task Root(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Root", ConsoleColor.Green);

            await _channel.Root(context);
        }

        private async Task Metadata(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Metadata", ConsoleColor.Green);

            await _channel.Metadata(context);
        }

        private async Task Count(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Count", ConsoleColor.Green);

            await CountImpl(context);
        }
        private async Task Search(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Search", ConsoleColor.Green);

            await SearchImpl(context);
        }

        private async Task CountImpl(InterceptCallContext context, string feed = null)
        {
            await _channel.Count(context, context.Args.SearchTerm, context.Args.IsLatestVersion, context.Args.TargetFramework, context.Args.IncludePrerelease, feed);
        }

        private async Task SearchImpl(InterceptCallContext context, string feed = null)
        {
            IDictionary<string, string> arguments = context.Args.Arguments;

            string searchTerm = context.Args.SearchTerm;
            bool isLatestVersion = context.Args.IsLatestVersion;
            string targetFramework = context.Args.TargetFramework;
            bool includePrerelease = context.Args.IncludePrerelease;
            int skip = context.Args.Skip ?? 30;
            int take = context.Args.Top ?? 30;

            await _channel.Search(context, searchTerm, isLatestVersion, targetFramework, includePrerelease, skip, take, feed);
        }

        private async Task FindPackagesById(InterceptCallContext context)
        {
            context.Log("[V3 CALL] FindPackagesById", ConsoleColor.Green);

            if (context.Args.Id == null)
            {
                throw new ShimException("unable to find id in query string");
            }

            if (context.Args.IsLatestVersion)
            {
                await _channel.GetLatestVersionPackage(context, context.Args.Id, context.Args.IncludePrerelease);
            }
            else
            {
                await _channel.GetAllPackageVersions(context, context.Args.Id);
            }
        }

        private async Task PackageIds(InterceptCallContext context)
        {
            context.Log("[V3 CALL] PackageIds", ConsoleColor.Green);

            await _channel.GetListOfPackages(context);
        }

        private async Task PackageVersions(InterceptCallContext context)
        {
            context.Log("[V3 CALL] PackageVersions", ConsoleColor.Green);

            string path = context.RequestUri.AbsolutePath;
            string id = path.Substring(path.LastIndexOf("/") + 1);

            await _channel.GetListOfPackageVersions(context, id);
        }

        private async Task GetUpdates(InterceptCallContext context)
        {
            context.Log("[V3 CALL] GetUpdates", ConsoleColor.Green);

            IDictionary<string, string> arguments = context.Args.Arguments;

            string[] packageIds = Uri.UnescapeDataString(arguments["packageIds"]).Trim('\'').Split('|');
            string[] versions = Uri.UnescapeDataString(arguments["versions"]).Trim('\'').Split('|');
            string[] versionConstraints = Uri.UnescapeDataString(arguments["versionConstraints"]).Trim('\'').Split('|');
            string[] targetFrameworks = Uri.UnescapeDataString(arguments["targetFrameworks"]).Trim('\'').Split('|');
            bool includeAllVersions = false;
            bool.TryParse(arguments["includeAllVersions"], out includeAllVersions);

            await _channel.GetUpdates(context, packageIds, versions, versionConstraints, targetFrameworks, context.Args.IncludePrerelease, includeAllVersions, context.Args.Count);
        }

        private async Task Packages(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Packages", ConsoleColor.Green);

            string path = Uri.UnescapeDataString(context.RequestUri.AbsolutePath);
            string query = context.RequestUri.Query;

            await GetPackage(context, path, query);
        }

        private async Task GetPackage(InterceptCallContext context, string path, string query, string feed = null)
        {
            if (path.EndsWith("Packages()"))
            {
                if(!String.IsNullOrEmpty(context.Args.FilterId))
                {
                    await _channel.GetAllPackageVersions(context, context.Args.FilterId.ToLowerInvariant());
                }
                else
                {
                    await _channel.ListAvailable(context);
                }
            }
            else
            {
                string args = path.Substring(path.LastIndexOf('(')).Trim('(', ')');

                string id = null;
                string version = null;

                string[] aps = args.Split(',');
                foreach (var ap in aps)
                {
                    string[] a = ap.Split('=');
                    if (a[0].Trim('\'') == "Id")
                    {
                        id = a[1].Trim('\'');
                    }
                    else if (a[0].Trim('\'') == "Version")
                    {
                        version = a[1].Trim('\'');
                    }
                }

                await _channel.GetPackage(context, id, version, feed);
            }
        }

        private async Task Feed_Metadata(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Feed_Metadata", ConsoleColor.Green);
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] feed: {0}", feed), ConsoleColor.DarkGreen);
            await _channel.Metadata(context, feed);
        }

        private async Task Feed_Count(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Feed_Count", ConsoleColor.Green);
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] feed: {0}", feed), ConsoleColor.DarkGreen);
            await CountImpl(context, feed);
        }

        private async Task Feed_Search(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Feed_Search", ConsoleColor.Green);
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] feed: {0}", feed), ConsoleColor.DarkGreen);
            await SearchImpl(context, feed);
        }

        private async Task Feed_FindPackagesById(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Feed_FindPackagesById", ConsoleColor.Green);
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] feed: {0}", ExtractFeed(context.RequestUri.AbsolutePath)), ConsoleColor.DarkGreen);

            await _channel.GetAllPackageVersions(context, context.Args.Id);
        }

        private async Task Feed_Packages(InterceptCallContext context)
        {
            context.Log("[V3 CALL] Feed_Packages", ConsoleColor.Green);
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            context.Log(string.Format(CultureInfo.InvariantCulture, "[V3 CALL] feed: {0}", feed), ConsoleColor.DarkGreen);

            string path = Uri.UnescapeDataString(context.RequestUri.AbsolutePath);
            path = path.Substring(path.IndexOf(feed) + feed.Length + 1);

            string query = context.RequestUri.Query;

            await GetPackage(context, path, query, feed);
        }

        private static string ExtractFeed(string path)
        {
            path = path.Remove(0, ShimConstants.V2UrlPath.Length);

            int index1 = path.IndexOf('/', 0) + 1;
            if (index1 < path.Length)
            {
                int index2 = path.IndexOf('/', index1);
                if (index2 < path.Length)
                {
                    string s = path.Substring(0, index2 + 1);
                    string[] t = s.Split('/');
                    if (t.Length > 1)
                    {
                        return t[1];
                    }
                }
            }
            return string.Empty;
        }
    }
}

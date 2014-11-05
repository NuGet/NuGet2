using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V3Shim
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
                new Tuple<string, Func<InterceptCallContext, Task>>("api/v2/package-ids", PackageIds),
                new Tuple<string, Func<InterceptCallContext, Task>>("api/v2/package-versions", PackageVersions),
                new Tuple<string, Func<InterceptCallContext, Task>>("$metadata", Metadata),
                new Tuple<string, Func<InterceptCallContext, Task>>("package", DownloadPackage),
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
        /// Process metrics for a call that may not be passed through Invoke.
        /// </summary>
        public async Task ReportMetrics(WebRequest request)
        {
            if (_initialized == true)
            {
                await Task.Run(() => _channel.ReportMetrics(request));
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

                // report metrics if the headers exist
                await ReportMetrics(context.Request);

                string unescapedAbsolutePath = Uri.UnescapeDataString(context.RequestUri.AbsolutePath);

                string path = unescapedAbsolutePath;

                // v2 is still in the path for get-package
                if (unescapedAbsolutePath.IndexOf(ShimConstants.V2UrlPath) == 0)
                {
                    path = unescapedAbsolutePath.Remove(0, ShimConstants.V2UrlPath.Length);
                }

                if (unescapedAbsolutePath.IndexOf(ShimConstants.V3UrlPath) == 0)
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
                V3InteropTraceSources.Dispatcher.Error("exception", "[{2}] {0} {1}", ex.GetType().ToString(), ex.Message, context.RequestUri.AbsoluteUri);
                throw;
            }
        }

        private async Task DownloadPackage(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

            var urlParts = new List<string>(context.RequestUri.AbsoluteUri.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            urlParts.Reverse();

            if (urlParts.Count > 3 && StringComparer.OrdinalIgnoreCase.Equals("package", urlParts[2]))
            {
                await _channel.DownloadPackage(context, urlParts[1], urlParts[0]);
            }
            else
            {
                throw new ShimException("Invalid download url: " + context.RequestUri.ToString());
            }
        }

        private async Task Root(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

            await _channel.Root(context);
        }

        private async Task Metadata(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

            await _channel.Metadata(context);
        }

        private async Task Count(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

            await CountImpl(context);
        }
        private async Task Search(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

            await SearchImpl(context);
        }

        private async Task CountImpl(InterceptCallContext context, string feed = null)
        {
            int skip = context.Args.Skip ?? 0;
            int take = context.Args.Top ?? 30;

            string sortBy = string.Empty;

            await _channel.SearchCount(context, context.Args.SearchTerm, context.Args.IsLatestVersion, context.Args.TargetFramework, context.Args.IncludePrerelease, skip, take, feed, sortBy);
        }

        private async Task SearchImpl(InterceptCallContext context, string feed = null)
        {
            int skip = context.Args.Skip ?? 0;
            int take = context.Args.Top ?? 30;

            string sortBy = string.Empty;

            await _channel.Search(context, context.Args.SearchTerm, context.Args.IsLatestVersion, context.Args.TargetFramework, context.Args.IncludePrerelease, skip, take, feed, sortBy);
        }

        private async Task FindPackagesById(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

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
            V3InteropTraceSources.Dispatcher.EnterMethod();

            await _channel.GetListOfPackages(context);
        }

        private async Task PackageVersions(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

            string path = context.RequestUri.AbsolutePath;
            string id = path.Substring(path.LastIndexOf("/") + 1);

            await _channel.GetListOfPackageVersions(context, id);
        }

        private async Task GetUpdates(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();

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
            V3InteropTraceSources.Dispatcher.EnterMethod();

            string path = Uri.UnescapeDataString(context.RequestUri.AbsolutePath);
            string query = context.RequestUri.Query;

            await GetPackage(context, path, query);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "query")]
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
            V3InteropTraceSources.Dispatcher.EnterMethod();
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            V3InteropTraceSources.Dispatcher.Verbose("metadata_feed", "Feed: {0}", feed);
            await _channel.Metadata(context, feed);
        }

        private async Task Feed_Count(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            V3InteropTraceSources.Dispatcher.Verbose("count_feed", "Feed: {0}", feed);
            await CountImpl(context, feed);
        }

        private async Task Feed_Search(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            V3InteropTraceSources.Dispatcher.Verbose("search_feed", "Feed: {0}", feed);
            await SearchImpl(context, feed);
        }

        private async Task Feed_FindPackagesById(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            V3InteropTraceSources.Dispatcher.Verbose("findpackagesbyid_feed", "Feed: {0}", feed);
            await _channel.GetAllPackageVersions(context, context.Args.Id);
        }

        private async Task Feed_Packages(InterceptCallContext context)
        {
            V3InteropTraceSources.Dispatcher.EnterMethod();
            string feed = ExtractFeed(context.RequestUri.AbsolutePath);
            V3InteropTraceSources.Dispatcher.Verbose("packages_feed", "Feed: {0}", feed);

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

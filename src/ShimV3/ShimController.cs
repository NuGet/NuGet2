using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace NuGet.ShimV3
{
    /// <summary>
    /// Contains the shim entry points.
    /// </summary>
    internal class ShimController : IShimController
    {
        private readonly List<InterceptDispatcher> _dispatchers;
        private readonly IDebugConsoleController _debugLogger;
        private IPackageSourceProvider _sourceProvider;
        private IShimCache _cache;

        public ShimController(IDebugConsoleController debugLogger)
        {
            _debugLogger = debugLogger;
            _dispatchers = new List<InterceptDispatcher>();
        }

        public void Enable(IPackageSourceProvider sourceProvider)
        {
            if (sourceProvider == null)
            {
                throw new ArgumentNullException("sourceProvider");
            }

            _sourceProvider = sourceProvider;

            CreateDispatchers();

            // add handlers to the Core shim
            HttpShim.Instance.SetDataServiceRequestHandler(ShimDataService);
            HttpShim.Instance.SetWebRequestHandler(ShimResponse);
        }

        public void UpdateSources()
        {
            if (_sourceProvider != null)
            {
                CreateDispatchers();
            }
        }

        public void Disable()
        {
            _sourceProvider = null;
            _dispatchers.Clear();

            // remove all handlers
            HttpShim.Instance.ClearHandlers();

            if (_cache != null)
            {
                _cache.Dispose();
                _cache = null;
            }
        }

        /// <summary>
        /// Main entry point. All requests pass through here except for ones skipped in ShimDataService().
        /// </summary>
        public WebResponse ShimResponse(WebRequest request)
        {
            Debug.Assert(request != null);
            WebResponse response = null;

            if (!TryGetInterceptorResponse(request, out response))
            {
                // Not handled by an interceptor, allow V2 to continue
                response = CallV2(request);
            }

            return response;
        }

        /// <summary>
        /// Entry point for requests from OData.
        /// </summary>
        public DataServiceClientRequestMessage ShimDataService(DataServiceClientRequestMessageArgs args)
        {
            DataServiceClientRequestMessage message = null;

            InterceptDispatcher dispatcher = GetDispatcher(args.RequestUri);

            if (dispatcher != null && dispatcher.Initialized == true)
            {
                // Let the interceptor handle this
                message = new ShimDataServiceClientRequestMessage(this, args);
            }

            // If no interceptors want the message create a normal HttpWebRequestMessage
            if (message == null)
            {
                Log(String.Format(CultureInfo.InvariantCulture, "[V2 REQ] {0}", args.RequestUri.AbsoluteUri), ConsoleColor.Gray);
                message = new HttpWebRequestMessage(args);
            }

            return message;
        }

        /// <summary>
        /// Create the dispatchers for v3 urls
        /// </summary>
        private void CreateDispatchers()
        {
            // add only new sources, old interceptors will stay around but not be used
            var newSources = _sourceProvider.LoadPackageSources().Where(s => s.IsEnabled && !_dispatchers.Any(d => AreSourcesEqual(d.Source, s.Source)));

            foreach (var newSource in newSources)
            {
                _dispatchers.Add(new InterceptDispatcher(newSource.Source, Cache));
            }
        }

        private IShimCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new ShimCache();
                }

                return _cache;
            }
        }

        private ShimWebResponse CallDispatcher(InterceptDispatcher dispatcher, WebRequest request)
        {
            ShimWebResponse response = null;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            try
            {
                using (var context = new ShimCallContext(request, _debugLogger))
                {
                    Log(String.Format(CultureInfo.InvariantCulture, "[V3 RUN] {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Yellow);
                    Task t = dispatcher.Invoke(context);
                    t.Wait();
                    var stream = context.Data;

                    timer.Stop();

                    Log(String.Format(CultureInfo.InvariantCulture, "[V3 END] {0}ms", timer.ElapsedMilliseconds), ConsoleColor.Yellow);

                    response = new ShimWebResponse(stream, request.RequestUri, context.ResponseContentType);
                }
            }
            catch (AggregateException ex)
            {
                // unwrap the exception to get a useful error
                var innerException = ExceptionUtility.Unwrap(ex);

                // TODO: throw a DataServiceQueryException with the correct xml
                throw innerException;
            }

            return response;
        }

        /// <summary>
        /// Pass the request to the interceptors. Interceptors will be initialized as needed.
        /// </summary>
        private bool TryGetInterceptorResponse(WebRequest request, out WebResponse response)
        {
            response = null;

            // find the correct dispatcher, only use it if it is initialized
            var dispatcher = GetDispatcher(request.RequestUri);

            if (dispatcher != null)
            {
                if (dispatcher.Initialized == true)
                {
                    response = CallDispatcher(dispatcher, request);
                }
                else if(dispatcher.Initialized == null && AreSourcesEqual(dispatcher.Source, request.RequestUri.AbsoluteUri))
                {
                    // root request, this is the first call to the source
                    try
                    {
                        response = request.GetResponse();

                        HttpWebResponse webResponse = response as HttpWebResponse;

                        if (webResponse != null && webResponse.StatusCode == HttpStatusCode.OK)
                        {
                            // this is a v2 source, do not use the shim
                            dispatcher.Initialized = false;
                        }
                    }
                    catch (WebException)
                    {
                        // expected
                        response = null;
                    }

                    // if the root document failed to load try to init
                    if (dispatcher.Initialized == null)
                    {
                        if (dispatcher.TryInit())
                        {
                            Log(String.Format(CultureInfo.InvariantCulture, "[V3 CHK] PASSED {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Yellow);

                            // init was successful, try again using the shim
                            response = CallDispatcher(dispatcher, request);
                        }
                        else
                        {
                            Log(String.Format(CultureInfo.InvariantCulture, "[V3 CHK] FAILED {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Gray);
                        }
                    }
                }
            }

            // intellisense scenario
            if (request.RequestUri.AbsoluteUri.IndexOf("/package-ids", StringComparison.OrdinalIgnoreCase) > -1 
                || request.RequestUri.AbsoluteUri.IndexOf("/package-versions", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var host = request.RequestUri.Host;

                var dispatchers = _dispatchers.Where(u => StringComparer.OrdinalIgnoreCase.Equals((new Uri(u.Source)).Host, host));

                // if v2 exists on this host, do not shim
                if (dispatchers.All(d => d.Initialized != false))
                {
                    var disp = dispatchers.Where(d => d.Initialized == true).FirstOrDefault();
                    response = CallDispatcher(disp, request);
                }
            }

            return response != null;
        }

        /// <summary>
        /// Get the response directly without the interceptor.
        /// </summary>
        private WebResponse CallV2(WebRequest request)
        {
            Log(String.Format(CultureInfo.InvariantCulture, "[V2 REQ] {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Gray);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            WebResponse response = request.GetResponse();

            timer.Stop();

            var httpResponse = response as HttpWebResponse;

            if (httpResponse != null)
            {
                Log(String.Format(CultureInfo.InvariantCulture, "[V2 RES] (status:{0}) (time:{1}ms) {2}",
                    httpResponse.StatusCode, timer.ElapsedMilliseconds, httpResponse.ResponseUri.AbsoluteUri),
                    httpResponse.StatusCode == HttpStatusCode.OK ? ConsoleColor.Gray : ConsoleColor.Red);
            }
            else
            {
                Log(String.Format(CultureInfo.InvariantCulture, "[V2 RES] (time:{0}ms) {1}", timer.ElapsedMilliseconds, response.ResponseUri.AbsoluteUri), ConsoleColor.Gray);
            }

            return response;
        }

        private void Log(string message, ConsoleColor color)
        {
            if (_debugLogger != null)
            {
                _debugLogger.Log(message, color);
            }
        }

        private InterceptDispatcher GetDispatcher(Uri uri)
        {
            return _dispatchers.Where(d => MatchesSource(d.Source, uri.AbsoluteUri)).FirstOrDefault();
        }

        private static bool AreSourcesEqual(string x, string y)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(x.TrimEnd('/'), y.TrimEnd('/'));
        }

        private static bool MatchesSource(string source, string requestUrl)
        {
            return requestUrl.StartsWith(source, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (_cache != null)
            {
                _cache.Dispose();
                _cache = null;
            }
        }
    }
}

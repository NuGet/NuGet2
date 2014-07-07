using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace NuGet.ShimV3
{
    /// <summary>
    /// Contains the shim entry points.
    /// </summary>
    internal class ShimController : IShimController
    {
        private List<Tuple<string, InterceptDispatcher>> _dispatchers;
        private IPackageSourceProvider _sourceProvider;
        private IDebugConsoleController _debugLogger;
        private IShimCache _cache;

        public ShimController(IDebugConsoleController debugLogger)
        {
            _debugLogger = debugLogger;
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

                // these need to be manually initialized
                foreach(var d in _dispatchers)
                {
                    d.Item2.TryInit();
                }
            }
        }

        public void Disable()
        {
            _sourceProvider = null;
            _dispatchers = null;

            // remove all handlers
            HttpShim.Instance.ClearHandlers();

            if (_cache != null)
            {
                _cache.Dispose();
                _cache = null;
            }
        }

        /// <summary>
        /// Create the dispatchers for v3 urls
        /// </summary>
        private void CreateDispatchers()
        {
            _dispatchers = new List<Tuple<string, InterceptDispatcher>>(1);

            foreach(var source in _sourceProvider.LoadPackageSources())
            {
                // add all enabled sources, we will check if they are really v3 later
                if (source.IsEnabled)
                {
                    var dispatcher = new InterceptDispatcher(source.Source, Cache);

                    _dispatchers.Add(new Tuple<string, InterceptDispatcher>(source.Source, dispatcher));
                }
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

        public WebResponse ShimResponse(WebRequest request)
        {
            Debug.Assert(request != null);
            Stopwatch timer = new Stopwatch();
            timer.Start();

            foreach (var dispatcher in _dispatchers)
            {
                // find the correct dispatcher, only use it if it is initialized
                if (dispatcher.Item2.Initialized == true && request.RequestUri.AbsoluteUri.StartsWith(dispatcher.Item1, StringComparison.OrdinalIgnoreCase))
                {
                    using (var context = new ShimCallContext(request, _debugLogger))
                    {
                        Log(String.Format(CultureInfo.InvariantCulture, "[V3 RUN] {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Yellow);

                        Task t = dispatcher.Item2.Invoke(context);
                        t.Wait();
                        var stream = context.Data;

                        timer.Stop();

                        Log(String.Format(CultureInfo.InvariantCulture, "[V3 END] {0}ms", timer.ElapsedMilliseconds), ConsoleColor.Yellow);

                        return new ShimWebResponse(stream, request.RequestUri, context.ResponseContentType);
                    }
                }
            }

            // Not handled by an interceptor, allow V2 to continue

            Log(String.Format(CultureInfo.InvariantCulture, "[V2 REQ] {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Gray);

            WebResponse response = null;

            try
            {
                response = request.GetResponse();
            }
            catch (WebException ex)
            {
                bool rethrow = true;

                // Dispatchers are initialized in two parts in an attempt to optimize this.
                // 1. Detect a 505 WebException from the root page (azure blobs)
                // 2. Check for intercept.json at the source url
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    foreach (var dispatcher in _dispatchers)
                    {
                        if (dispatcher.Item2.Initialized == null && request.RequestUri.AbsoluteUri.TrimEnd('/').Equals(dispatcher.Item1.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                        {
                            if (dispatcher.Item2.TryInit())
                            {
                                Log(String.Format(CultureInfo.InvariantCulture, "[V3 CHK] PASSED {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Yellow);
                                rethrow = false;

                                // let's try that again and use the fake root
                                response = ShimResponse(request);
                            }
                            else
                            {
                                Log(String.Format(CultureInfo.InvariantCulture, "[V3 CHK] FAILED {0}", request.RequestUri.AbsoluteUri), ConsoleColor.Gray);
                            }
                        }
                    }
                }

                if (rethrow)
                {
                    throw;
                }
            }

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

        public DataServiceClientRequestMessage ShimDataService(DataServiceClientRequestMessageArgs args)
        {
            DataServiceClientRequestMessage message = null;

            // Check if an interceptor wants the message
            foreach (var dispatcher in _dispatchers)
            {
                if (dispatcher.Item2.Initialized == true && args.RequestUri.AbsoluteUri.StartsWith(dispatcher.Item1, StringComparison.OrdinalIgnoreCase))
                {
                    message = new ShimDataServiceClientRequestMessage(this, args);
                }
            }

            // If no interceptors want the message create a normal HttpWebRequestMessage
            if (message == null)
            {
                Log(String.Format(CultureInfo.InvariantCulture, "[V2 REQ] {0}", args.RequestUri.AbsoluteUri), ConsoleColor.Gray);
                message = new HttpWebRequestMessage(args);
            }

            return message;
        }

        private void Log(string message, ConsoleColor color)
        {
            if (_debugLogger != null)
            {
                _debugLogger.Log(message, color);
            }
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

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace NuGet.Client.V3Shim
{
    /// <summary>
    /// Contains the shim entry points.
    /// </summary>
    internal class ShimController : IShimController
    {
        private readonly List<InterceptDispatcher> _dispatchers;
        private IPackageSourceProvider _sourceProvider;
        private IShimCache _cache;
        private InterceptDispatcher _lastUsedDispatcher;

        public ShimController()
        {
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
        /// 
        public WebResponse ShimResponse(WebRequest request)
        {
            return ShimResponse(request, null);
        }

        public WebResponse ShimResponse(WebRequest request, MemoryStream requestStream)
        {
            Debug.Assert(request != null);
            WebResponse response = null;

            // pass the request to the interceptors
            if (!TryGetInterceptorResponse(request, requestStream, out response))
            {
                // Not handled by an interceptor, allow V2 to continue
                response = CallV2(request);

                // The metrics for downloads may come through on the CDN url, which is not handled
                // by the dispatcher. We need to give it a chance to log this info.
                CallDispatcherForMetrics(_lastUsedDispatcher, request);

                _lastUsedDispatcher = null;
            }

            return response;
        }

        /// <summary>
        /// Entry point for requests from OData.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.Client.V3Shim.ShimController.Log(System.String,System.ConsoleColor)")]
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
                V3InteropTraceSources.ShimController.Verbose("request", "{0} {1}", args.Method, args.RequestUri.AbsoluteUri);
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

        private static void CallDispatcherForMetrics(InterceptDispatcher dispatcher, WebRequest request)
        {
            if (dispatcher != null)
            {
                try
                {
                    // do not wait for this
                    Task.Run(async () => await dispatcher.ReportMetrics(request));
                }
                catch (AggregateException ex)
                {
                    // unwrap the exception to get a useful error
                    var innerException = ExceptionUtility.Unwrap(ex);

                    // TODO: throw a DataServiceQueryException with the correct xml
                    throw innerException;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ms"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.Client.V3Shim.ShimController.Log(System.String,System.ConsoleColor)")]
        private ShimWebResponse CallDispatcher(InterceptDispatcher dispatcher, WebRequest request, MemoryStream requestStream)
        {
            ShimWebResponse response = null;
            _lastUsedDispatcher = dispatcher;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            try
            {
                using (var context = new ShimCallContext(request, requestStream))
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(request.Method, "POST") && requestStream != null)
                    {
                        // convert the batched call into a regular url
                        context.UnBatch();
                    }

                    V3InteropTraceSources.ShimController.Verbose("dispatching", "Dispatching {0} {1}", request.Method, request.RequestUri.AbsoluteUri);

                    var t = Task.Run(async () => await dispatcher.Invoke(context));
                    t.Wait();

                    var stream = context.Data;

                    timer.Stop();

                    V3InteropTraceSources.ShimController.Verbose("dispatched", "Dispatched {0} {1} in {2}ms", request.Method, request.RequestUri.AbsoluteUri, timer.ElapsedMilliseconds);

                    response = new ShimWebResponse(stream, request.RequestUri, context.ResponseContentType, context.StatusCode);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.Client.V3Shim.ShimController.Log(System.String,System.ConsoleColor)")]
        private bool TryGetInterceptorResponse(WebRequest request, MemoryStream requestStream, out WebResponse response)
        {
            response = null;

            // find the correct dispatcher, only use it if it is initialized
            var dispatcher = GetDispatcher(request.RequestUri);

            if (dispatcher != null)
            {
                if (dispatcher.Initialized == true)
                {
                    response = CallDispatcher(dispatcher, request, requestStream);
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
                            V3InteropTraceSources.ShimController.Info("dispatcher_init", "Dispatcher initialized. {0} is a V3 Feed", request.RequestUri.AbsoluteUri);
                            
                            // init was successful, try again using the shim
                            response = CallDispatcher(dispatcher, request, requestStream);
                        }
                        else
                        {
                            V3InteropTraceSources.ShimController.Info("dispatcher_fail", "Dispatcher failed to initialize. {0} is a V2 Feed", request.RequestUri.AbsoluteUri);
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
                    response = CallDispatcher(disp, request, requestStream);
                }
            }

            return response != null;
        }

        /// <summary>
        /// Get the response directly without the interceptor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ms"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "NuGet.Client.V3Shim.ShimController.Log(System.String,System.ConsoleColor)")]
        private static WebResponse CallV2(WebRequest request)
        {
            V3InteropTraceSources.ShimController.Verbose("v2_request", "V2 {0} {1}", request.Method, request.RequestUri.AbsoluteUri);
                
            Stopwatch timer = new Stopwatch();
            timer.Start();

            WebResponse response = request.GetResponse();

            timer.Stop();

            var httpResponse = response as HttpWebResponse;

            if (httpResponse != null)
            {
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    V3InteropTraceSources.ShimController.Verbose("v2_response", "V2 {0} {1} ({2}ms)", httpResponse.StatusCode, request.RequestUri.AbsoluteUri, timer.ElapsedMilliseconds);
                }
                else
                {
                    V3InteropTraceSources.ShimController.Error("v2_response", "V2 {0} {1} ({2}ms)", httpResponse.StatusCode, request.RequestUri.AbsoluteUri, timer.ElapsedMilliseconds);
                }
            }
            else
            {
                V3InteropTraceSources.ShimController.Verbose("v2_response", "V2 ? {0} ({1}ms)", request.RequestUri.AbsoluteUri, timer.ElapsedMilliseconds);
            }

            return response;
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

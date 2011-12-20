using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;
using System.Diagnostics.CodeAnalysis;

namespace NuGet
{
    public class PackageServer : IPackageServer
    {
        private const string ServiceEndpoint = "/api/v2/package";
        private const string ApiKeyHeader = "X-NuGet-ApiKey";
        private static readonly HttpStatusCode[] AcceptableStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.Created };

        private readonly Lazy<Uri> _baseUri;
        private readonly string _source;
        private readonly string _userAgent;

        public PackageServer(string source, string userAgent)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
            }
            _source = source;
            _userAgent = userAgent;
            _baseUri = new Lazy<Uri>(ResolveBaseUrl);
        }

        public string Source
        {
            get { return _source; }
        }

        public void PushPackage(string apiKey, Stream packageStream)
        {
            HttpClient client = GetClient("", "PUT", "application/octet-stream");

            client.SendingRequest += (sender, e) =>
            {
                var request = (HttpWebRequest)e.Request;

                // Set the timeout to the same as the read write timeout (5 mins is the default)
                request.Timeout = request.ReadWriteTimeout;
                request.Headers.Add(ApiKeyHeader, apiKey);

                var multiPartRequest = new MultipartWebRequest();
                multiPartRequest.AddFile(() => packageStream, "package");

                multiPartRequest.CreateMultipartRequest(request);
            };

            EnsureSuccessfulResponse(client);
        }

        public void DeletePackage(string apiKey, string packageId, string packageVersion)
        {
            // Review: Do these values need to be encoded in any way?
            var url = String.Join("/", packageId, packageVersion);
            HttpClient client = GetClient(url, "DELETE", "text/html");
            
            client.SendingRequest += (sender, e) =>
            {
                var request = (HttpWebRequest)e.Request;
                request.Headers.Add(ApiKeyHeader, apiKey);
            };
            EnsureSuccessfulResponse(client);
        }

        private HttpClient GetClient(string path, string method, string contentType)
        {
            var baseUrl = _baseUri.Value;
            Uri requestUri = GetServiceEndpointUrl(baseUrl, path);

            var client = new HttpClient(requestUri)
            {
                ContentType = contentType,
                Method = method
            };

            if (!String.IsNullOrEmpty(_userAgent))
            {
                client.UserAgent = HttpUtility.CreateUserAgentString(_userAgent);
            }

            return client;
        }

        internal static Uri GetServiceEndpointUrl(Uri baseUrl, string path)
        {
            Uri requestUri;
            if (String.IsNullOrEmpty(baseUrl.AbsolutePath.TrimStart('/')))
            {
                // If there's no host portion specified, append the url to the client.
                requestUri = new Uri(baseUrl, ServiceEndpoint + '/' + path);
            }
            else
            {
                requestUri = new Uri(baseUrl, path);
            }
            return requestUri;
        }

        private static void EnsureSuccessfulResponse(HttpClient client)
        {
            WebResponse response = null;
            try
            {
                response = client.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    throw;
                }

                response = e.Response;

                var httpResponse = (HttpWebResponse)e.Response;
                if (httpResponse != null && !AcceptableStatusCodes.Contains(httpResponse.StatusCode))
                {
                    string body = ReadResponseBody(httpResponse);
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageServerError, httpResponse.StatusDescription, body), e);
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        private Uri ResolveBaseUrl()
        {
            Uri uri = null;

            try
            {
                var client = new RedirectedHttpClient(new Uri(Source));
                uri = client.Uri;
            }
            catch (WebException ex)
            {
                var response = (HttpWebResponse)ex.Response;
                if (response == null)
                {
                    throw;
                }

                uri = response.ResponseUri;
            }

            return EnsureTrailingSlash(uri);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want to throw an exception from inside an exception")]
        private static string ReadResponseBody(HttpWebResponse response)
        {
            try
            {
                return response.GetResponseStream().ReadToEnd();
            }
            catch
            {
                // We don't want to throw an exception when trying to read the exceptional response's body.
                return String.Empty;
            }
        }

        private static Uri EnsureTrailingSlash(Uri uri)
        {
            string value = uri.OriginalString;
            if (!value.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                value += "/";
            }
            return new Uri(value);
        }
    }
}

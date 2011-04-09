using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace NuGet.Repositories
{
    public class HttpClientFactory
    {
        private static readonly HttpClientFactory _default = new HttpClientFactory();
        private const string _UserAgentPattern = "NuGet Package Explorer/{0} ({1})";
        private static readonly Version _version = typeof(GalleryServer).Assembly.GetNameSafe().Version;
        private static readonly string _userAgent = String.Format(CultureInfo.InvariantCulture, _UserAgentPattern, _version, Environment.OSVersion);


        public virtual IHttpClient CreateClient(Uri uri)
        {
            return new HttpClient(uri);
        }

        public static HttpClientFactory Default {
            get { return _default; }
        }
    }
}

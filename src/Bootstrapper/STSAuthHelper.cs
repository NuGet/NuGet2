using System;
using System.Net;

namespace NuGet
{
    public static class STSAuthHelper
    {
        public static void PrepareSTSRequest(WebRequest request)
        {
            // Do nothing. Duh!
        }

        public static bool TryRetrieveSTSToken(Uri uri, IHttpWebResponse response)
        {
            return false;
        }
    }
}

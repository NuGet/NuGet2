using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.ShimV3
{
    public class InterceptorArguments
    {
        public InterceptorArguments(Uri uri)
        {
            Parse(uri);
        }

        public IDictionary<string, string> Arguments { get; private set; }

        public bool IsLatestVersion { get; set; }

        public string Id { get; set; }

        public bool IncludePrerelease { get; set; }

        public int? Top { get; private set; }

        public int? Skip { get; private set; }

        public string SkipToken { get; private set; }

        public string TargetFramework { get; private set; }

        public string FilterId { get; private set; }

        public string FilterStartsWithId { get; private set; }

        public string OrderBy { get; private set; }

        public string SearchTerm { get; private set; }

        public string PartialId { get; private set; }

        public bool Count { get; private set; }

        public bool HasFilter
        {
            get
            {
                return Arguments.ContainsKey("$filter");
            }
        }

        private void Parse(Uri uri)
        {
            IDictionary<string, string> arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] args = uri.Query.TrimStart('?').Split('&');
            foreach (var arg in args)
            {
                if (arg.IndexOf("=", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    string[] val = arg.Split('=');
                    arguments[val[0]] = Uri.UnescapeDataString(val.Length > 0 ? val[1] : string.Empty);
                }
            }
            Arguments = arguments;

            Count = uri.AbsoluteUri.IndexOf("/$count", StringComparison.OrdinalIgnoreCase) > -1;

            foreach(string key in arguments.Keys)
            {
                string value = arguments[key];

                switch (key.ToLowerInvariant())
                {
                    case "targetframework":
                        TargetFramework = Uri.UnescapeDataString(value).Trim('\'');
                        break;
                    case "includeprerelease":
                        IncludePrerelease = GetBool(value) == true;
                        break;
                    case "$skip":
                        Skip = GetInt(value);
                        break;
                    case "$top":
                        Top = GetInt(value);
                        break;
                    case "$filter":
                        ParseFilter(value);
                        break;
                    case "$skiptoken":
                        SkipToken = Uri.UnescapeDataString(value);
                        break;
                    case "$orderby":
                        OrderBy = Uri.UnescapeDataString(value);
                        break;
                    case "id":
                        Id = Uri.UnescapeDataString(value).Trim('\'');
                        break;
                    case "searchterm":
                        SearchTerm = Uri.UnescapeDataString(value).Trim('\'');
                        break;
                    case "partialid":
                        PartialId = Uri.UnescapeDataString(value).Trim('\'');
                        break;
                    default:
                        // Debug.Fail("Unhandled arg: " + key);
                        break;
                }
            }
        }

        private void ParseFilter(string value)
        {
            string[] filters = Uri.UnescapeDataString(value).Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var f in filters)
            {
                string filter = f.Trim();

                if (StringComparer.InvariantCultureIgnoreCase.Equals(filter, "IsLatestVersion"))
                {
                    IsLatestVersion = true;
                }
                else if (StringComparer.InvariantCultureIgnoreCase.Equals(filter, "IsAbsoluteLatestVersion"))
                {
                    IsLatestVersion = true;
                    IncludePrerelease = true;
                }

                if (StringComparer.InvariantCultureIgnoreCase.Equals(filter, "IsLatestAbsoluteVersion"))
                {
                    IsLatestVersion = true;
                    IncludePrerelease = true;
                }

                if (filter.IndexOf(" eq ", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    FilterId = filter.Substring(filter.IndexOf("eq", StringComparison.OrdinalIgnoreCase) + 2).Trim(' ', '\'');
                }

                if (filter.IndexOf("startswith(tolower(Id),'", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    FilterStartsWithId = filter.Split('\'')[1];
                }
            }
        }

        private static int? GetInt(string value)
        {
            int x = 0;
            if (int.TryParse(Uri.UnescapeDataString(value), out x))
            {
                return x;
            }

            return null;
        }

        private static bool? GetBool(string value)
        {
            bool b = false;
            if (bool.TryParse(Uri.UnescapeDataString(value), out b))
            {
                return b;
            }

            return null;
        }
    }
}

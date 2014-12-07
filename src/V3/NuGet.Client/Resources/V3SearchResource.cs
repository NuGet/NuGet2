using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.V3;
using System.Runtime.Versioning;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace NuGet.Client.Resources
{
    //TODO : Pass host name;
    public class V3SearchResource : SearchResource
    {
        private string _url;
        private NuGetV3Client _client;
        public override string Url
        {
            get { return _url; }
        }

        public V3SearchResource(string sourceUrl)
        {
            _url = sourceUrl;
            _client = new NuGetV3Client(sourceUrl, "xxx");
            
        }

        public async override Task<IEnumerable<VisualStudioUISearchResult>> GetSearchResultsForVisualStudioUI(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {

            List<string> frameworkNames = new List<string>();
            foreach (FrameworkName fx in filters.SupportedFrameworks)
                frameworkNames.Add(VersionUtility.GetShortFrameworkName(fx));
            IEnumerable<JObject> searchResultJsonObjects =    await _client.Search(searchTerm, frameworkNames, filters.IncludePrerelease, skip, take, cancellationToken);
            List<VisualStudioUISearchResult> visualStudioUISearchResults = new List<VisualStudioUISearchResult>();
            foreach (JObject searchResultJson in searchResultJsonObjects)
                visualStudioUISearchResults.Add(GetVisualStudioUISearchResult(searchResultJson));
            return visualStudioUISearchResults;
        }

        public override Task<IEnumerable<CommandLineSearchResult>> GetSearchResultsForCommandLine(string searchTerm, bool includePrerelease, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<PowershellSearchResult>> GetSearchResultsForPowershellConsole(string searchTerm, SearchFilter filters, int skip, int take, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private VisualStudioUISearchResult GetVisualStudioUISearchResult(JObject package)
        {
             VisualStudioUISearchResult searchResult = new VisualStudioUISearchResult();
              searchResult.Id = package.Value<string>(Properties.PackageId);
              searchResult.Version = NuGetVersion.Parse(package.Value<string>(Properties.LatestVersion));
              searchResult.IconUrl = GetUri(package, Properties.IconUrl);

                    // get other versions
                    var versionList = new List<NuGetVersion>();
                    var versions = package.Value<JArray>(Properties.Versions);
                    if (versions != null)
                    {
                        if (versions[0].Type == JTokenType.String)
                        {
                            // TODO: this part should be removed once the new end point is up and running.
                            versionList = versions
                                .Select(v => NuGetVersion.Parse(v.Value<string>()))
                                .ToList();
                        }
                        else
                        {
                            versionList = versions
                                .Select(v => NuGetVersion.Parse(v.Value<string>("version")))
                                .ToList();
                        }
                    }
                    searchResult.Summary = package.Value<string>(Properties.Summary);
                    if (string.IsNullOrWhiteSpace(searchResult.Summary))
                    {
                        // summary is empty. Use its description instead.
                        searchResult.Summary = package.Value<string>(Properties.Description);
                    }
                    return searchResult;

        }
        private Uri GetUri(JObject json, string property)
        {
            if (json[property] == null)
            {
                return null;
            }
            string str = json[property].ToString();
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }
            return new Uri(str);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using NuGet.VisualStudio;
using Resx = NuGet.Client.VisualStudio.UI.Resources;
using NuGet.ProjectManagement;

namespace NuGet.Client.VisualStudio.UI
{
    internal enum Filter
    {
        All,
        Installed,
        UpdatesAvailable
    }

    internal class PackageLoaderOption
    {
        public PackageLoaderOption(
            Filter filter,
            bool includePrerelease)
        {
            Filter = filter;
            IncludePrerelease = includePrerelease;
        }

        public Filter Filter { get; private set; }

        public bool IncludePrerelease { get; private set; }
    }

    internal class PackageLoader : ILoader
    {
        // the installation target
        private NuGetProject _target;

        private PackageLoaderOption _option;

        // the currently selected source
        private SourceRepository _source;

        private string _searchText;

        private const int _pageSize = 10;

        // Copied from file Constants.cs in NuGet.Core:
        // This is temporary until we fix the gallery to have proper first class support for this.
        // The magic unpublished date is 1900-01-01T00:00:00
        public static readonly DateTimeOffset Unpublished = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));

        public PackageLoader(
            SourceRepository source,
            NuGetProject target,
            PackageLoaderOption option,
            string searchText)
        {
            _target = target;
            _option = option;
            _source = source;
            _searchText = searchText;

            LoadingMessage = string.IsNullOrWhiteSpace(searchText) ?
                Resx.Resources.Text_Loading :
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resx.Resources.Text_Searching,
                    searchText);
        }

        public string LoadingMessage
        {
            get;
            private set;
        }

        private async Task<IEnumerable<UISearchMetadata>> Search(int startIndex, CancellationToken ct)
        {
            if (_option.Filter == Filter.Installed ||
                _option.Filter == Filter.UpdatesAvailable)
            {
                // search in target
                var packages = await _target.SearchInstalled(
                    _source,
                    _searchText,
                    startIndex,
                    _pageSize,
                    ct);
                return packages;
            }
            else
            {
                // search in source
                if (_source == null)
                {
                    return Enumerable.Empty<UISearchMetadata>();
                }
                else
                {
                    return _source.GetResource<V3UISearchResource>();
                }
            }
        }

        public async Task<LoadResult> LoadItems(int startIndex, CancellationToken ct)
        {
            List<UiSearchResultPackage> packages = new List<UiSearchResultPackage>();
            var results = await Search(startIndex, ct);
            int resultCount = 0;
            foreach (var package in results)
            {
                ct.ThrowIfCancellationRequested();
                ++resultCount;

                var searchResultPackage = new UiSearchResultPackage(_source);
                searchResultPackage.Id = package.Id;
                searchResultPackage.Version = package.Version;
                searchResultPackage.IconUrl = package.IconUrl;

                // get other versions
                var versionList = package.Versions.ToList();
                if (!_option.IncludePrerelease)
                {
                    // remove prerelease version if includePrelease is false
                    versionList.RemoveAll(v => v.IsPrerelease);
                }

                if (!versionList.Contains(searchResultPackage.Version))
                {
                    versionList.Add(searchResultPackage.Version);
                }

                searchResultPackage.Versions = versionList;
                searchResultPackage.Status = PackageManagerControl.GetPackageStatus(
                    searchResultPackage.Id,
                    _target,
                    searchResultPackage.Versions);

                // filter out prerelease version when needed.
                if (searchResultPackage.Version.IsPrerelease &&
                   !_option.IncludePrerelease &&
                    searchResultPackage.Status == PackageStatus.NotInstalled)
                {
                    continue;
                }

                if (_option.Filter == Filter.UpdatesAvailable &&
                    searchResultPackage.Status != PackageStatus.UpdateAvailable)
                {
                    continue;
                }

                searchResultPackage.Summary = package.Summary;
                packages.Add(searchResultPackage);
            }

            ct.ThrowIfCancellationRequested();
            return new LoadResult()
            {
                Items = packages,
                HasMoreItems = resultCount == _pageSize,
                NextStartIndex = startIndex + resultCount
            };
        }

        // Get all versions of the package
        private List<UiPackageMetadata> LoadVersions(JArray versions, NuGetVersion searchResultVersion)
        {
            var retValue = new List<UiPackageMetadata>();
            if (versions == null)
            {
                return retValue;
            }

            // If repo is AggregateRepository, the package duplicates can be returned by
            // FindPackagesById(), so Distinct is needed here to remove the duplicates.
            foreach (var token in versions)
            {
                Debug.Assert(token.Type == JTokenType.Object);
                JObject version = (JObject)token;
                var detailedPackage = DetailControlModel.CreateDetailedPackage(version);

                if (detailedPackage.Version.IsPrerelease &&
                    !_option.IncludePrerelease &&
                    detailedPackage.Version != searchResultVersion)
                {
                    // don't include prerelease version if includePrerelease is false
                    continue;
                }

                if (detailedPackage.Published <= Unpublished &&
                    detailedPackage.Version != searchResultVersion)
                {
                    // don't include unlisted package
                    continue;
                }

                retValue.Add(detailedPackage);
            }

            return retValue;
        }

        private static Uri GetUri(JObject json, string property)
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
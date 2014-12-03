using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Client.Installation;
using NuGet.Client.Resolution;
using NuGet.Client;

namespace NuGet.Client.Interop
{
    public class V2SourceRepository : SourceRepository
    {
        private readonly IPackageRepository _repository;
        private readonly LocalPackageRepository _lprepo;
        private readonly PackageSource _source;
        private readonly string _userAgent;

        public override PackageSource Source { get { return _source; } }

        public V2SourceRepository(PackageSource source, IPackageRepository repository, string host)
        {
            _source = source;
            _repository = repository;

            // TODO: Get context from current UI activity (PowerShell, Dialog, etc.)
            _userAgent = UserAgentUtil.GetUserAgent("NuGet.Client.Interop", host);

            var events = _repository as IHttpClientEvents;
            if (events != null)
            {
                events.SendingRequest += (sender, args) =>
                {
                    var httpReq = args.Request as HttpWebRequest;
                    if (httpReq != null)
                    {
                        httpReq.UserAgent = _userAgent;
                    }
                    NuGetTraceSources.V2SourceRepository.Verbose("http", "{0} {1}", args.Request.Method, args.Request.RequestUri.ToString());
                };
            }

            _lprepo = _repository as LocalPackageRepository;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public override Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            NuGetTraceSources.V2SourceRepository.Verbose("search", "Searching for '{0}'", searchTerm);
            return Task.Factory.StartNew(() =>
            {
                var query = _repository.Search(
                    searchTerm,
                    filters.SupportedFrameworks.Select(fx => fx.FullName),
                    filters.IncludePrerelease);

                // V2 sometimes requires that we also use an OData filter for latest/latest prerelease version
                if (filters.IncludePrerelease)
                {
                    query = query.Where(p => p.IsAbsoluteLatestVersion);
                }
                else
                {
                    query = query.Where(p => p.IsLatestVersion);
                }

                if (_repository is LocalPackageRepository)
                {
                    // if the repository is a local repo, then query contains all versions of packages.
                    // we need to explicitly select the latest version.
                    query = query.OrderBy(p => p.Id)
                        .ThenByDescending(p => p.Version)
                        .GroupBy(p => p.Id)
                        .Select(g => g.First());
                }

                // Now apply skip and take and the rest of the party
                return (IEnumerable<JObject>)query
                    .Skip(skip)
                    .Take(take)
                    .ToList()
                    .AsParallel()
                    .AsOrdered()
                    .Select(p => CreatePackageSearchResult(p, cancellationToken))
                    .ToList();
            }, cancellationToken);
        }

        private JObject CreatePackageSearchResult(IPackage package, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            NuGetTraceSources.V2SourceRepository.Verbose("getallvers", "Retrieving all versions for {0}", package.Id);
            var versions = _repository.FindPackagesById(package.Id);
            if (!versions.Any())
            {
                versions = new[] { package };
            }

            return PackageJsonLd.CreatePackageSearchResult(package, versions.Select(p => p.Version));
        }

        public override Task<JObject> GetPackageMetadata(string id, Versioning.NuGetVersion version)
        {
            return Task.Factory.StartNew(() =>
            {
                NuGetTraceSources.V2SourceRepository.Verbose("getpackage", "Getting metadata for {0} {1}", id, version);
                var semver = CoreConverters.SafeToSemVer(version);
                var package = _repository.FindPackage(id, semver);

                // Sometimes, V2 APIs seem to fail to return a value for Packages(Id=,Version=) requests...
                if (package == null)
                {
                    var packages = _repository.FindPackagesById(id);
                    package = packages.FirstOrDefault(p => Equals(p.Version, semver));
                }

                // If still null, fail
                if (package == null)
                {
                    return null;
                }

                string repoRoot = null;
                IPackagePathResolver resolver = null;
                if (_lprepo != null)
                {
                    repoRoot = _lprepo.Source;
                    resolver = _lprepo.PathResolver;
                }

                return PackageJsonLd.CreatePackage(package, repoRoot, resolver);
            });
        }

        public override Task<IEnumerable<JObject>> GetPackageMetadataById(string packageId)
        {
            return Task.Factory.StartNew(() =>
            {
                NuGetTraceSources.V2SourceRepository.Verbose("findpackagebyid", "Getting metadata for all versions of {0}", packageId);
                string repoRoot = null;
                IPackagePathResolver resolver = null;
                if (_lprepo != null)
                {
                    repoRoot = _lprepo.Source;
                    resolver = _lprepo.PathResolver;
                }
                return _repository.FindPackagesById(packageId).Select(p => PackageJsonLd.CreatePackage(p, repoRoot, resolver));
            });
        }

        public override void RecordMetric(PackageActionType actionType, PackageIdentity packageIdentity, PackageIdentity dependentPackage, bool isUpdate, IInstallationTarget target)
        {
            // No-op, V2 doesn't support this.
        }
    }
}
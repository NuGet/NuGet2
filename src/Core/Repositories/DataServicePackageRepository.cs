using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public class DataServicePackageRepository : PackageRepositoryBase, IHttpClientEvents, IServiceBasedRepository, ICloneableRepository, ICultureAwareRepository, IOperationAwareRepository
    {
        private IDataServiceContext _context;
        private readonly IHttpClient _httpClient;
        private readonly PackageDownloader _packageDownloader;
        private CultureInfo _culture;
        private const string FindPackagesByIdSvcMethod = "FindPackagesById";
        private const string SearchSvcMethod = "Search";
        private const string GetUpdatesSvcMethod = "GetUpdates";

        // Just forward calls to the package downloader
        public event EventHandler<ProgressEventArgs> ProgressAvailable
        {
            add
            {
                _packageDownloader.ProgressAvailable += value;
            }
            remove
            {
                _packageDownloader.ProgressAvailable -= value;
            }
        }

        public event EventHandler<WebRequestEventArgs> SendingRequest
        {
            add
            {
                _packageDownloader.SendingRequest += value;
                _httpClient.SendingRequest += value;
            }
            remove
            {
                _packageDownloader.SendingRequest -= value;
                _httpClient.SendingRequest -= value;
            }
        }

        public PackageDownloader PackageDownloader
        {
            get { return _packageDownloader; }
        }

        public string CurrentOperation { get; private set; }

        public DataServicePackageRepository(Uri serviceRoot)
            : this(new HttpClient(serviceRoot))
        {
        }

        public DataServicePackageRepository(IHttpClient client)
            : this(client, new PackageDownloader())
        {
        }

        public DataServicePackageRepository(IHttpClient client, PackageDownloader packageDownloader)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }
            if (packageDownloader == null)
            {
                throw new ArgumentNullException("packageDownloader");
            }

            _httpClient = client;
            _httpClient.AcceptCompression = true;

            _packageDownloader = packageDownloader;

            SendingRequest += (sender, e) =>
            {
                if (!String.IsNullOrEmpty(CurrentOperation))
                {
                    e.Request.Headers[RepositoryOperationNames.OperationHeaderName] = CurrentOperation;
                }
            };
        }

        public CultureInfo Culture
        {
            get
            {
                if (_culture == null)
                {
                    // TODO: Technically, if this is a remote server, we have to return the culture of the server
                    // instead of invariant culture. However, there is no trivial way to retrieve the server's culture,
                    // So temporarily use Invariant culture here. 
                    _culture = _httpClient.Uri.IsLoopback ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture;
                }
                return _culture;
            }
        }

        public override string Source
        {
            get
            {
                return _httpClient.Uri.OriginalString;
            }
        }

        public override bool SupportsPrereleasePackages
        {
            get
            {
                return Context.SupportsProperty("IsAbsoluteLatestVersion");
            }
        }

        // Don't initialize the Context at the constructor time so that
        // we don't make a web request if we are not going to actually use it
        // since getting the Uri property of the RedirectedHttpClient will
        // trigger that functionality.
        internal IDataServiceContext Context
        {
            private get
            {
                if (_context == null)
                {
                    _context = new DataServiceContextWrapper(_httpClient.Uri);
                    _context.SendingRequest += OnSendingRequest;
                    _context.ReadingEntity += OnReadingEntity;
                    _context.IgnoreMissingProperties = true;
                }
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        private void OnReadingEntity(object sender, ReadingWritingEntityEventArgs e)
        {
            var package = (DataServicePackage)e.Entity;

            // REVIEW: This is the only way (I know) to download the package on demand
            // GetReadStreamUri cannot be evaluated inside of OnReadingEntity. Lazily evaluate it inside DownloadPackage
            package.Context = Context;
            package.Downloader = _packageDownloader;
        }

        private void OnSendingRequest(object sender, SendingRequestEventArgs e)
        {
            // Initialize the request
            _httpClient.InitializeRequest(e.Request);
        }

        public override IQueryable<IPackage> GetPackages()
        {
            // REVIEW: Is it ok to assume that the package entity set is called packages?
            return new SmartDataServiceQuery<DataServicePackage>(Context, Constants.PackageServiceEntitySetName);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            if (!Context.SupportsServiceMethod(SearchSvcMethod))
            {
                // If there's no search method then we can't filter by target framework
                return GetPackages().Find(searchTerm)
                                    .FilterByPrerelease(allowPrereleaseVersions)
                                    .AsQueryable();
            }

            // Convert the list of framework names into short names
            var shortFrameworkNames = targetFrameworks.Select(name => new FrameworkName(name))
                                                      .Select(VersionUtility.GetShortFrameworkName);

            // Create a '|' separated string of framework names
            string targetFrameworkString = String.Join("|", shortFrameworkNames);

            var searchParameters = new Dictionary<string, object> {
                { "searchTerm", "'" + Escape(searchTerm) + "'" },
                { "targetFramework", "'" + Escape(targetFrameworkString) + "'" },
            };

            if (SupportsPrereleasePackages)
            {
                searchParameters.Add("includePrerelease", ToString(allowPrereleaseVersions));
            }

            // Create a query for the search service method
            var query = Context.CreateQuery<DataServicePackage>(SearchSvcMethod, searchParameters);
            return new SmartDataServiceQuery<DataServicePackage>(Context, query);
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            if (!Context.SupportsServiceMethod(FindPackagesByIdSvcMethod))
            {
                // If there's no search method then we can't filter by target framework
                return PackageRepositoryExtensions.FindPackagesByIdCore(this, packageId);
            }

            var serviceParameters = new Dictionary<string, object> {
                { "id", "'" + Escape(packageId) + "'" }
            };

            // Create a query for the search service method
            var query = Context.CreateQuery<DataServicePackage>(FindPackagesByIdSvcMethod, serviceParameters);
            return new SmartDataServiceQuery<DataServicePackage>(Context, query);
        }

        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> targetFramework)
        {
            if (!Context.SupportsServiceMethod(GetUpdatesSvcMethod))
            {
                // If there's no search method then we can't filter by target framework
                return PackageRepositoryExtensions.GetUpdatesCore(this, packages, includePrerelease, includeAllVersions, targetFramework);
            }

            // Pipe all the things!
            string ids = String.Join("|", packages.Select(p => p.Id));
            string versions = String.Join("|", packages.Select(p => p.Version.ToString()));
            string targetFrameworkValue = targetFramework.IsEmpty() ? "" : String.Join("|", targetFramework.Select(VersionUtility.GetShortFrameworkName));

            var serviceParameters = new Dictionary<string, object> {
                { "packageIds", "'" + ids + "'" },
                { "versions", "'" + versions + "'" },
                { "includePrerelease", ToString(includePrerelease) },
                { "includeAllVersions", ToString(includeAllVersions) },
                { "targetFrameworks", "'" + Escape(targetFrameworkValue) + "'" },
               
            };

            var query = Context.CreateQuery<DataServicePackage>(GetUpdatesSvcMethod, serviceParameters);
            return new SmartDataServiceQuery<DataServicePackage>(Context, query);
        }

        public IPackageRepository Clone()
        {
            return new DataServicePackageRepository(_httpClient, _packageDownloader);
        }

        public IDisposable StartOperation(string operation)
        {
            string oldOperation = CurrentOperation;
            CurrentOperation = operation;
            return new DisposableAction(() =>
            {
                CurrentOperation = oldOperation;
            });
        }

        /// <summary>
        /// Escapes single quotes in the value by replacing them with two single quotes.
        /// </summary>
        private static string Escape(string value)
        {
            // REVIEW: Couldn't find another way to do this with odata
            if (!String.IsNullOrEmpty(value))
            {
                return value.Replace("'", "''");
            }

            return value;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "OData expects a lower case value.")]
        private static string ToString(bool value)
        {
            return value.ToString().ToLowerInvariant();
        }
    }
}
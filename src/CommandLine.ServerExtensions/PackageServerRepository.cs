using System;
using System.Linq;

namespace NuGet.ServerExtensions
{
    /// <summary>
    /// Thin wrapper that allows exposing a PackageServer as an IPackageRepository
    /// </summary>
    public class PackageServerRepository : IPackageRepository
    {
        private readonly IPackageRepository _source;
        private readonly PackageServer _destination;
        private readonly string _apiKey;
        private readonly TimeSpan _timeout;
        private readonly ILogger _logger;

        public PackageServerRepository(IPackageRepository sourceRepository, PackageServer destination, string apiKey, TimeSpan timeout, ILogger logger)
        {
            if (sourceRepository == null)
            {
                throw new ArgumentNullException("sourceRepository");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            
            _source = sourceRepository;
            _destination = destination;
            _apiKey = apiKey;
            _timeout = timeout;
            _logger = logger ?? NullLogger.Instance;
        }

        public string Source
        {
            get { return _source.Source; }
        }

        // PackageSaveMode property does not apply to this class
        public PackageSaveModes PackageSaveMode
        {
            set { throw new NotSupportedException(); }
            get { throw new NotSupportedException(); }
        }

        public bool SupportsPrereleasePackages
        {
            get { return _source.SupportsPrereleasePackages; }
        }

        public IQueryable<IPackage> GetPackages()
        {
            return _source.GetPackages();
        }

        public void AddPackage(IPackage package)
        {
            _logger.Log(MessageLevel.Info, NuGetResources.MirrorCommandPushingPackage, package.GetFullName(), CommandLineUtility.GetSourceDisplayName(_destination.Source));
            _destination.PushPackage(
                _apiKey, 
                package, 
                package.GetStream().Length, 
                (int)_timeout.TotalMilliseconds, 
                disableBuffering: false);
            _logger.Log(MessageLevel.Info, NuGetResources.MirrorCommandPackagePushed);
        }

        public void RemovePackage(IPackage package)
        {
            throw new NotSupportedException();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Windows;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPackageRepositoryFactory))]
    [Export(typeof(IProgressProvider))]
    [Export(typeof(IHttpClientEvents))]
    public class CachedRepositoryFactory : 
        IPackageRepositoryFactory, 
        IProgressProvider, 
        IHttpClientEvents,
        IWeakEventListener
    {
        private readonly ConcurrentDictionary<string, IPackageRepository> _repositoryCache = new ConcurrentDictionary<string, IPackageRepository>();
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;
        
        public event EventHandler<ProgressEventArgs> ProgressAvailable = delegate { };
        public event EventHandler<WebRequestEventArgs> SendingRequest = delegate { };

        [ImportingConstructor]
        public CachedRepositoryFactory(IPackageSourceProvider packageSourceProvider)
            : this(PackageRepositoryFactory.Default, packageSourceProvider)
        {
        }

        internal CachedRepositoryFactory(IPackageRepositoryFactory repositoryFactory,
                                         IPackageSourceProvider packageSourceProvider)
        {
            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }

            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }

            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public IPackageRepository CreateRepository(string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source"),
                    "source");
            }

            // aggregate source, return aggregate repository
            if (AggregatePackageSource.Instance.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase) ||
                AggregatePackageSource.Instance.Name.Equals(source, StringComparison.CurrentCultureIgnoreCase))
            {
                //Set ignoreFailingRepositories to true. Fixes bug http://nuget.codeplex.com/workitem/3777
                //Also, in the dialog when All is chosen and some repositories are failing, we will show aggregate from successful sources
                return _packageSourceProvider.CreateAggregateRepository(this, ignoreFailingRepositories: true);
            }

            // try to resolve the name or feed from the source 
            return GetPackageRepository(_packageSourceProvider.ResolveSource(source));
        }

        private IPackageRepository GetPackageRepository(string source)
        {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(source, out repository))
            {
                repository = _repositoryFactory.CreateRepository(source);
                _repositoryCache.TryAdd(source, repository);

                // See if this repository provides progress
                var progressProvider = repository as IProgressProvider;
                if (progressProvider != null)
                {
                    progressProvider.ProgressAvailable += OnProgressAvailable;
                }

                var httpEvents = repository as IHttpClientEvents;
                if (httpEvents != null)
                {
                    SendingRequestEventManager.AddListener(httpEvents, this);
                }
            }
            return repository;
        }

        private void OnProgressAvailable(object sender, ProgressEventArgs e)
        {
            ProgressAvailable(this, e);
        }

        private void OnSendingRequest(object sender, WebRequestEventArgs e)
        {
            SendingRequest(this, e);
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(SendingRequestEventManager))
            {
                OnSendingRequest(sender, (WebRequestEventArgs)e);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
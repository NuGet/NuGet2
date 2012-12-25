using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using NuGet.VisualStudio;

namespace NuGet.PowerShell.Commands
{
    public abstract class JsonApiCommandBase<T> : NuGetBaseCommand where T : class
    {
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        protected JsonApiCommandBase()
            : this(
                ServiceLocator.GetInstance<ISolutionManager>(),
                ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                ServiceLocator.GetInstance<IHttpClientEvents>(),
                ServiceLocator.GetInstance<IPackageRepositoryFactory>(),
                ServiceLocator.GetInstance<IVsPackageSourceProvider>())
        {
        }

        protected JsonApiCommandBase(
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory,
            IHttpClientEvents httpClientEvents,
            IPackageRepositoryFactory repositoryFactory,
            IVsPackageSourceProvider packageSourceProvider)
            : base(solutionManager, packageManagerFactory, httpClientEvents)
        {
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public abstract string ApiEndpointPath { get; }
        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        [Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        protected abstract Dictionary<string, string> BuildApiEndpointQueryParameters();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Too much logic for getter.")]
        protected virtual IPackageRepository GetPackageRepository()
        {
            if (!String.IsNullOrEmpty(Source))
            {
                return CreateRepositoryFromSource(_repositoryFactory, _packageSourceProvider, Source);
            }

            if (_packageSourceProvider.ActivePackageSource != null)
            {
                return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource.Source);
            }

            throw new InvalidOperationException(Resources.Cmdlet_NoActivePackageSource);
        }
        
        protected abstract IEnumerable<T> GetResultsFromPackageRepository(IPackageRepository packageRepository);
        
        protected virtual IEnumerable<T> GetResults(Uri apiEndpointUri)
        {
            var jsonSerializer = new DataContractJsonSerializer(typeof(T[]));
            var httpClient = new HttpClient(apiEndpointUri);
            using (var stream = new MemoryStream())
            {
                httpClient.DownloadData(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return jsonSerializer.ReadObject(stream) as T[];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification="Wrapping the string in a new URI doesn't improve anything.")]
        protected override void ProcessRecordCore()
        {
            var packageRepository = GetPackageRepository();

            var aggregatePackageRepository = packageRepository as AggregateRepository;
            if (aggregatePackageRepository != null)
            {
                WriteResults(AggregateResults(aggregatePackageRepository));
            }
            else
            {
                WriteResults(GetResults(packageRepository));
            }
        }

        private IEnumerable<T> AggregateResults(AggregateRepository aggregatePackageRepository)
        {
            var tasks = aggregatePackageRepository.Repositories
                .Select(r => Task.Factory.StartNew(() => GetResults(r)))
                .ToArray();
            Task.WaitAll(tasks);
            return tasks
                .SelectMany(t => t.Result)
                .Distinct()
                .Take(30);
        }

        private static string BuildQueryString(Dictionary<string, string> queryParameters)
        {
            if (queryParameters.Count == 0)
            {
                return string.Empty;
            }

            return String.Join("&", queryParameters.Select(param => string.Format(CultureInfo.InvariantCulture, "{0}={1}", param.Key, Uri.EscapeDataString(param.Value))));
        }

        private IEnumerable<T> GetResults(IPackageRepository packageRepository)
        {
            Debug.Assert(!(packageRepository is AggregateRepository), "This should never be called for an aggregate package repository.");

            if (!UriHelper.IsHttpSource(packageRepository.Source))
            {
                return GetResultsFromPackageRepository(packageRepository);
            }
            else
            {
                var queryParameters = BuildApiEndpointQueryParameters() ?? new Dictionary<string, string>();
                if (IncludePrerelease)
                {
                    queryParameters.Add("includePrerelease", "true");
                }

                var uriBuilder = new UriBuilder(packageRepository.Source)
                {
                    Path = ApiEndpointPath,
                    Query = BuildQueryString(queryParameters)
                };

                return GetResults(uriBuilder.Uri);
            }
        }
        
        private void WriteResults(IEnumerable<T> results)
        {
            foreach (var result in results)
            {
                WriteObject(result);
            }
        }
    }
}
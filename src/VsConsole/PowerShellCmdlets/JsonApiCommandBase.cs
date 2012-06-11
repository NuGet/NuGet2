using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Management.Automation;
using System.Runtime.Serialization.Json;
using System.Text;
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

        protected abstract NameValueCollection BuildApiEndpointQueryParameters();

        private static string BuildQueryString(NameValueCollection queryParameters)
        {
            if (queryParameters.Count == 0)
            {
                return string.Empty;
            }

            var queryStringBuilder = new StringBuilder();
            foreach(var key in queryParameters.Keys)
            {
                queryStringBuilder.AppendFormat("{0}={1}&", key, Uri.EscapeDataString(queryParameters[key.ToString()]));
            }

            // remove the final &
            queryStringBuilder.Length--;

            return queryStringBuilder.ToString();
        }

        protected abstract IEnumerable<T> GetResultsFromPackageRepository(IPackageRepository packageRepository);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification="Too much logic for getter.")]
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
        
        protected virtual T[] GetResults(Uri apiEndpointUri)
        {
            var jsonSerializer = new DataContractJsonSerializer(typeof(T[]));
            var httpClient = new HttpClient(apiEndpointUri);
            using (var stream = new MemoryStream(httpClient.DownloadData()))
            {
                return jsonSerializer.ReadObject(stream) as T[];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification="Wrapping the string in a new URI doesn't improve anything.")]
        protected override void ProcessRecordCore()
        {
            var packageRepository = GetPackageRepository();

            if (!packageRepository.Source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                foreach(var result in GetResultsFromPackageRepository(packageRepository))
                {
                    WriteObject(result);
                }
            }
            
            var queryParameters = BuildApiEndpointQueryParameters() ?? new NameValueCollection();
            if (IncludePrerelease)
            {
                queryParameters.Add("includePrerelease", "true");
            }

            var uriBuilder = new UriBuilder(packageRepository.Source)
            {
                Path = ApiEndpointPath,
                Query = BuildQueryString(queryParameters)
            };

            foreach (var result in GetResults(uriBuilder.Uri))
            {
                WriteObject(result);
            }
        }
    }
}
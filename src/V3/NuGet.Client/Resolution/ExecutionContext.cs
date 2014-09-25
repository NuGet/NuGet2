using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resolution
{
    public class ExecutionContext
    {
        private Func<Uri, IHttpClient> _httpClientFactory;

        public IProjectManager ProjectManager { get; private set; }
        public IPackageManager PackageManager { get; private set; }
        public IPackageCacheRepository PackageCache { get; private set; }
        public PackageDownloader PackageDownloader { get; private set; }
        public bool SupportsBindingRedirects
        {
            get
            {
                return PackageManager.BindingRedirectEnabled && ProjectManager.Project.IsBindingRedirectSupported;
            }
        }

        public ExecutionContext(ProjectInstallationTarget target) : this(
            target.ProjectManager,
            target.ProjectManager.PackageManager,
            MachineCache.Default,
            new PackageDownloader(),
            u => new HttpClient(u)) { }

        public ExecutionContext(
            IProjectManager projectManager,
            IPackageManager packageManager,
            IPackageCacheRepository packageCache)
            : this(projectManager, packageManager, packageCache, new PackageDownloader(), u => new HttpClient(u)) { }

        public ExecutionContext(
            IProjectManager projectManager,
            IPackageManager packageManager,
            IPackageCacheRepository packageCache,
            PackageDownloader packageDownloader,
            Func<Uri, IHttpClient> httpClientFactory)
        {
            // No nulls! Ever!
            if (projectManager == null)
            {
                throw new ArgumentNullException("projectManager");
            }
            if (packageManager == null)
            {
                throw new ArgumentNullException("packageManager");
            }
            if (packageCache == null)
            {
                throw new ArgumentNullException("packageCache");
            }
            if (packageDownloader == null)
            {
                throw new ArgumentNullException("packageDownloader");
            }
            if (httpClientFactory == null)
            {
                throw new ArgumentNullException("httpClientFactory");
            }

            ProjectManager = projectManager;
            PackageManager = packageManager;
            PackageCache = packageCache;
            PackageDownloader = packageDownloader;

            _httpClientFactory = httpClientFactory;
        }

        public IHttpClient CreateHttpClient(Uri downloadUri)
        {
            return _httpClientFactory(downloadUri);
        }
    }
}

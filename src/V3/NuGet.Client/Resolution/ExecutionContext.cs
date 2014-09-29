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
        private CoreInteropInstallationTargetBase _target;

        public IPackageManager PackageManager { get; private set; }
        public IPackageCacheRepository PackageCache { get; private set; }
        public PackageDownloader PackageDownloader { get; private set; }

        public ExecutionContext(CoreInteropInstallationTargetBase target) : this(
            target,
            MachineCache.Default,
            new PackageDownloader(),
            u => new HttpClient(u)) { }

        public ExecutionContext(
            CoreInteropInstallationTargetBase target,
            IPackageCacheRepository packageCache)
            : this(target, packageCache, new PackageDownloader(), u => new HttpClient(u)) { }

        public ExecutionContext(
            CoreInteropInstallationTargetBase target,
            IPackageCacheRepository packageCache,
            PackageDownloader packageDownloader,
            Func<Uri, IHttpClient> httpClientFactory)
        {
            // No nulls! Ever!
            if (target == null)
            {
                throw new ArgumentNullException("target");
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

            _target = target;

            PackageManager = _target.GetPackageManager();
            PackageCache = packageCache;
            PackageDownloader = packageDownloader;

            _httpClientFactory = httpClientFactory;
        }

        public IProjectManager GetProjectManager(TargetProject project)
        {
            return _target.GetProjectManager(project);
        }

        public IHttpClient CreateHttpClient(Uri downloadUri)
        {
            return _httpClientFactory(downloadUri);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Installation;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    public class VsInstallationHost : InstallationHost
    {
        private readonly IVsPackageManager _packageManager;

        private readonly CoreInteropFeature _coreInteropFeature;

        public VsInstallationHost(IVsPackageManager packageManager)
        {
            _packageManager = packageManager;

            _coreInteropFeature = new CoreInteropFeature(
                _packageManager,
                project => _packageManager.GetProjectManager(((VsTargetProject)project).Project),
                MachineCache.Default,
                new PackageDownloader(),
                uri => new HttpClient(uri));
        }
        
        public override object TryGetFeature(Type featureType)
        {
            if (featureType == typeof(CoreInteropFeature))
            {
                return _coreInteropFeature;
            }
            return null;
        }
    }
}

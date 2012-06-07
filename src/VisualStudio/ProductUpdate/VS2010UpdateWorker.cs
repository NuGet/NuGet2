using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionManager;
using NuGet.VisualStudio10;

namespace NuGet.VisualStudio
{
    internal class VS2010UpdateWorker : IUpdateWorker
    {
        private const string NuGetVSIXId = "NuPackToolsVsix.Microsoft.67e54e40-0ae3-42c5-a949-fddf5739e7a5";
        private readonly IVsExtensionRepository _extensionRepository;
        private readonly IVsExtensionManager _extensionManager;

        public VS2010UpdateWorker() :
            this(ServiceLocator.GetGlobalService<SVsExtensionRepository, IVsExtensionRepository>(),
                 ServiceLocator.GetGlobalService<SVsExtensionManager, IVsExtensionManager>())
        {
        }

        public VS2010UpdateWorker(IVsExtensionRepository extensionRepository, IVsExtensionManager extensionManager)
        {
            if (extensionManager == null)
            {
                throw new ArgumentNullException("extensionManager");
            }

            if (extensionRepository == null)
            {
                throw new ArgumentNullException("extensionRepository");
            }

            _extensionManager = extensionManager;
            _extensionRepository = extensionRepository;
        }

        public bool CheckForUpdate(out Version installedVersion, out Version newVersion)
        {
            // Find the vsix on the vs gallery
            // IMPORTANT: The .AsEnumerble() call is REQUIRED. Don't remove it or the update service won't work.
            GalleryEntry nugetVsix = _extensionRepository.CreateQuery<GalleryEntry>(includeTypeInQuery: false, includeSkuInQuery: true)
                                                      .Where(e => e.VsixID == NuGetVSIXId)
                                                      .AsEnumerable()
                                                      .FirstOrDefault();
            // Get the current NuGet VSIX version
            IInstalledExtension installedNuGet = _extensionManager.GetInstalledExtension(NuGetVSIXId);
            installedVersion = installedNuGet.Header.Version;

            // If we're running an older version then update
            if (nugetVsix != null && nugetVsix.NonNullVsixVersion > installedVersion)
            {
                newVersion = nugetVsix.NonNullVsixVersion;
                return true;
            }
            else
            {
                newVersion = installedVersion;
                return false;
            }
        }
    }
}
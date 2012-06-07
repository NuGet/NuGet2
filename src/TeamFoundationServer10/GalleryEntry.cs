using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.TeamFoundationServer10
{
    /// <summary>
    /// This class replicates the Microsoft.VisualStudio.ExtensionManager.UI.VsGalleryEntry in Microsoft.VisualStudio.ExtensionsManager.Implementation.dll.
    /// We do so to avoid dependency on Implementation.dll assembly, which is a private assembly of VS.
    /// </summary>
    public class GalleryEntry : IRepositoryEntry
    {
        private Version _nonNullVsixVersion;

        public string VsixID
        {
            get;
            set;
        }

        public string DownloadUrl
        {
            get;
            set;
        }

        public string VsixReferences
        {
            get;
            set;
        }

        public string VsixVersion
        {
            get;
            set;
        }

        public Version NonNullVsixVersion
        {
            get
            {
                if (_nonNullVsixVersion == null)
                {
                    if (!Version.TryParse(VsixVersion, out _nonNullVsixVersion))
                    {
                        _nonNullVsixVersion = new Version();
                    }

                }

                return _nonNullVsixVersion;
            }
        }
    }
}

using NuGet.Common;

namespace NuGet.MSBuild {
    public class GalleryServerFactory : IGalleryServerFactory {
        public IGalleryServer createFrom(string source) {
            return new GalleryServer(source);
        }
    }
}
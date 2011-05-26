using NuGet.Common;

namespace NuGet.MSBuild {
    public interface IGalleryServerFactory {
        IGalleryServer createFrom(string source);
    }
}
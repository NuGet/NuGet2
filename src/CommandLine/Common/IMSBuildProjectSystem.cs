using System.IO;

namespace NuGet.Common {
    public interface IMSBuildProjectSystem : IProjectSystem {
        void Save();
    }
}

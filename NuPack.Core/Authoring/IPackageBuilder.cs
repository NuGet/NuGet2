using System.Collections.ObjectModel;
using System.IO;

namespace NuPack {
    public interface IPackageBuilder : IPackageMetadata {
        Collection<IPackageFile> Files { get; }
        void Save(Stream stream);
    }
}

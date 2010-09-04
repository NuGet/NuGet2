using System.Collections.Generic;

namespace NuPack {
    // TODO: Flesh this interface out so these can be registered externally. Right now these
    // methods are based on what we do for config
    internal interface IPackageFileModifier {
        void Modify(IPackageFile file, ProjectSystem projectSystem);
        void Revert(IPackageFile file, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem);
    }
}

using System.Collections.Generic;
using System.IO;

namespace NuPack {
    public interface IPackageFileTransformer {
        /// <summary>
        /// Transforms the file
        /// </summary>
        void TransformFile(IPackageFile file, string targetPath, ProjectSystem projectSystem, IPackageEventListener listener);

        /// <summary>
        /// Reverses the transform
        /// </summary>
        void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem, IPackageEventListener listener);
    }
}

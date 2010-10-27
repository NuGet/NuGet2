using System.Collections.Generic;
using System.IO;

namespace NuGet {
    public interface IPackageFileTransformer {
        /// <summary>
        /// Transforms the file
        /// </summary>
        void TransformFile(IPackageFile file, string targetPath, ProjectSystem projectSystem);

        /// <summary>
        /// Reverses the transform
        /// </summary>
        void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem);
    }
}

using System.IO;
using System.Reflection;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {
    [PackageContentViewerMetadata(0, ".dll", ".exe")]
    internal class AssemblyFileViewer : IPackageContentViewer {
        public object GetView(Stream stream) {
            string tempFile = Path.GetTempFileName();
            using (FileStream fileStream = File.OpenWrite(tempFile)) {
                stream.CopyTo(fileStream);
            }

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(tempFile);
            string fullName = assemblyName.FullName;

            try {
                File.Delete(tempFile);
            }
            catch { 
            }

            return fullName;
        }
    }
}
using System;
using System.Runtime.InteropServices;
using EnvDTE;

namespace NuGet.VisualStudio {
    [ComImport]
    [Guid("4F3B122B-A53B-432C-8D85-0FAFB8BE4FF4")]
    public interface IVsPackageInstaller {
        void InstallPackage(string source, Project project, string packageId, Version version, bool ignoreDependencies);
    }
}

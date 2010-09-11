using System;
using Microsoft.VisualStudio.Shell.Interop;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuPack.Dialog.PackageProject {
    class ProjectFactoryBaseInterfaces : IVsProjectFactory, IVsProjectFactory2 {
        int IVsProjectFactory.CanCreateProject(string pszFilename, uint grfCreateFlags, out int pfCanCreate) {
            throw new NotImplementedException();
        }

        int IVsProjectFactory.Close() {
            throw new NotImplementedException();
        }

        int IVsProjectFactory.CreateProject(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled) {
            throw new NotImplementedException();
        }

        int IVsProjectFactory.SetSite(IOLEServiceProvider psp) {
            throw new NotImplementedException();
        }

        int IVsProjectFactory2.GetAsynchOpenProjectType(out uint pType) {
            throw new NotImplementedException();
        }
    }
}

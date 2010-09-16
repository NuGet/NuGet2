using System;
using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NuPack.VisualStudio {
    public static class DTEExtensions {
        public static T GetService<T>(this _DTE dte, Type serviceType)
            where T : class {
            // Get the service provider from dte
            IServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider.GetService<T>(serviceType);
        }

        public static T GetService<T>(this IServiceProvider serviceProvider, Type serviceType)
            where T : class {
            return (T)serviceProvider.GetService(serviceType);
        }
    }
}

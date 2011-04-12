using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuGet.VisualStudio {
    /// <summary>
    /// This class unifies all the different ways of getting services within visual studio.
    /// </summary>
    // REVIEW: Make this internal 
    public static class ServiceLocator {

        public static TService GetInstance<TService>() where TService : class {
            // Special case IServiceProvider
            if (typeof(TService) == typeof(IServiceProvider)) {
                return (TService)GetServiceProvider();
            }

            // then try to find the service as a global service, then try dte then try component model
            return GetGlobalService<TService, TService>() ??
                   GetDTEService<TService>() ??
                   GetComponentModelService<TService>();
        }

        public static TInterface GetGlobalService<TService, TInterface>() {
            return (TInterface)Package.GetGlobalService(typeof(TService));
        }

        private static TService GetDTEService<TService>() where TService : class {
            var dte = GetGlobalService<SDTE, DTE>();
            return (TService)QueryService(dte, typeof(TService));
        }

        private static TService GetComponentModelService<TService>() where TService : class {
            IComponentModel componentModel = GetGlobalService<SComponentModel, IComponentModel>();
            return componentModel.GetService<TService>();
        }

        private static IServiceProvider GetServiceProvider() {
            var dte = GetGlobalService<SDTE, DTE>();
            return GetServiceProvider(dte);
        }

        private static object QueryService(_DTE dte, Type serviceType) {
            Guid guidService = serviceType.GUID;
            Guid riid = guidService;
            var serviceProvider = dte as VsServiceProvider;

            IntPtr servicePtr;
            int hr = serviceProvider.QueryService(ref guidService, ref riid, out servicePtr);

            if (hr != VsConstants.S_OK) {
                // We didn't find the service so return null
                return null;
            }

            object service = null;

            if (servicePtr != IntPtr.Zero) {
                service = Marshal.GetObjectForIUnknown(servicePtr);
                Marshal.Release(servicePtr);
            }

            return service;

        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller is responsible for disposing this")]
        private static IServiceProvider GetServiceProvider(_DTE dte) {
            IServiceProvider serviceProvider = new ServiceProvider(dte as VsServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider;
        }
    }
}
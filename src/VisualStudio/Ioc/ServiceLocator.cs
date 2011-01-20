using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuGet.VisualStudio {
    public static class ServiceLocator {
        private static CompositionContainer _container;
        private static IEnumerable<Func<Type, object>> _fallBackServiceLocators;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static void Initialize(Package package, params Assembly[] assemblies) {
            if (_container != null) {
                return;
            }

            var assemblyCatalogs = assemblies.Select(a => new AssemblyCatalog(a));
            var catalog = new AggregateCatalog(assemblyCatalogs);
            _container = new CompositionContainer(catalog);

            var serviceProvider = (IServiceProvider)package;
            var dte = (DTE)serviceProvider.GetService(typeof(SDTE));
            IServiceProvider dteServiceProvider = GetServiceProvider(dte);

            // Add DTE
            _container.ComposeExportedValue(dte);
            // Add DTE's IServiceProvider
            _container.ComposeExportedValue(dteServiceProvider);
            // Add the package to the container
            _container.ComposeExportedValue(package);

            // MEF within VS
            var componentModel = (IComponentModel)dteServiceProvider.GetService(typeof(SComponentModel));
            _container.ComposeExportedValue(componentModel);
            
            // Setup fallback service locators when the default container fails
            _fallBackServiceLocators = new Func<Type, object>[] { 
                                       type => dteServiceProvider.GetService(type),
                                       type => serviceProvider.GetService(type), 
                                       type => QueryService(dte, type)
                                     };
        }

        public static T GetInstance<T>() where T : class {
            return _container.GetExportedValueOrDefault<T>() ??
                   (T)_fallBackServiceLocators.Select(locator => locator(typeof(T)))
                                      .FirstOrDefault();

        }

        public static T GetInstance<T>(string contractName) where T : class {
            return _container.GetExportedValueOrDefault<T>(contractName) ??
                   (T)_fallBackServiceLocators.Select(locator => locator(typeof(T)))
                                      .FirstOrDefault();
        }

        private static object QueryService(_DTE dte, Type serviceType) {
            Guid guidService = serviceType.GUID;
            Guid riid = guidService;
            var serviceProvider = dte as VsServiceProvider;

            IntPtr servicePtr;
            int hr = serviceProvider.QueryService(ref guidService, ref riid, out servicePtr);

            if (hr != VsConstants.S_OK) {
                Marshal.ThrowExceptionForHR(hr);
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

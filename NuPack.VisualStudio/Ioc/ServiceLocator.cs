using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using EnvDTE;

namespace NuGet.VisualStudio {
    public class ServiceLocator {
        private static ServiceLocator _instance;
        
        private readonly CompositionContainer _container;
        public ServiceLocator(CompositionContainer container) {
            _container = container;
        }

        public static void Initialize(DTE dte) {
            if (_instance != null) {
                return;
            }

            var catalog = new AssemblyCatalog(typeof(ServiceLocator).Assembly);
            var container = new CompositionContainer(catalog);

            // Add DTE and DTE's IServiceProvider to the container
            container.ComposeExportedValue(dte);
            container.ComposeExportedValue(dte.GetServiceProvider());

            _instance = new ServiceLocator(container);
        }

        public static T GetInstance<T>() {
            return _instance._container.GetExportedValueOrDefault<T>();
        }
    }
}

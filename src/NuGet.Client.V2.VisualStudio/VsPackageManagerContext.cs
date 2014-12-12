using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DteSolution = EnvDTE.Solution;
using Microsoft.VisualStudio.Shell;
using NuGet.Client.ProjectSystem;
using NuGet.VisualStudio;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.IO;

namespace NuGet.Client.VisualStudio
{
    [Export(typeof(VsPackageManagerContext))]
    public class VsPackageManagerContext : PackageManagerContext
    {
        private readonly EnvDTE._DTE _dte;
        private readonly SourceRepositoryManager _sourceManager;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsPackageManagerFactory _packageManagerFactory;

        public override SourceRepositoryManager SourceManager
        {
            get { return _sourceManager; }
        }

        [ImportingConstructor]
        public VsPackageManagerContext(
            SourceRepositoryManager sourceManager,
            SVsServiceProvider serviceProvider,
            ISolutionManager solutionManager,
            IVsPackageManagerFactory packageManagerFactory)
        {
            _sourceManager = sourceManager;
            _solutionManager = solutionManager;
            _packageManagerFactory = packageManagerFactory;

            _dte = (EnvDTE._DTE)serviceProvider.GetService(typeof(EnvDTE._DTE));
        }

        public override Solution GetCurrentSolution()
        {
            return GetCurrentVsSolution();
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "It is not idempotent. Each call generates a new object.")]
        public virtual VsSolution GetCurrentVsSolution()
        {
            if (!_solutionManager.IsSolutionOpen)
            {
                return null;
            }

            return new VsSolution(
                _dte.Solution,
                _solutionManager,
                _packageManagerFactory.CreatePackageManagerToManageInstalledPackages());
        }

        public void AssembleCalculatorComponents()
        {
            try
            {
                //Creating an instance of aggregate catalog. It aggregates other catalogs
                var aggregateCatalog = new AggregateCatalog();

                //Build the directory path where the parts will be available
                var directoryPath =@"C:\Users\bhuvak\AppData\Local\Microsoft\VisualStudio\12.0Exp\Extensions\Outercurve Foundation\NuGet Package Manager\3.0.0.0";
                   

                //Load parts from the available DLLs in the specified path 
                //using the directory catalog
                var directoryCatalog = new DirectoryCatalog(directoryPath, "*.dll");

                //Load parts from the current assembly if available
                var asmCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

                //Add to the aggregate catalog
                aggregateCatalog.Catalogs.Add(directoryCatalog);
                aggregateCatalog.Catalogs.Add(asmCatalog);

                //Crete the composition container
                var container = new CompositionContainer(aggregateCatalog);

                // Composable parts are created here i.e. 
                // the Import and Export components assembles here
                container.ComposeParts(this);
                container.GetExports<VsPackageManagerContext>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

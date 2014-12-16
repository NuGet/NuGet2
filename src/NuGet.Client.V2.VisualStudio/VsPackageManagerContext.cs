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
        }
    }
}

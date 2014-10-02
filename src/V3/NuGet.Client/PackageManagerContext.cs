using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.ProjectSystem;

namespace NuGet.Client
{
    public abstract class PackageManagerContext
    {
        /// <summary>
        /// Gets the source manager used to manage active and available package repositories
        /// </summary>
        public abstract SourceRepositoryManager SourceManager { get; }

        /// <summary>
        /// Gets the currently active solution.
        /// </summary>
        public abstract Solution GetCurrentSolution();
    }
}

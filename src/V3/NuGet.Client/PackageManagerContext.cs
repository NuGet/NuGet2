using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "It is not idempotent. Each call generates a new object.")]
        public abstract Solution GetCurrentSolution();
    }
}

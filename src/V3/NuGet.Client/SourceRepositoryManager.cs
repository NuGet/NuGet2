using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    /// <summary>
    /// Manages a collection of source repositories and the currently-active one
    /// </summary>
    public abstract class SourceRepositoryManager
    {
        public abstract SourceRepository ActiveRepository { get; }

        public abstract event EventHandler PackageSourcesChanged;

        public abstract SourceRepository CreateSourceRepository(PackageSource packageSource);

        public abstract IEnumerable<PackageSource> AvailableSources { get; }

        /// <summary>
        /// Changes the active source to the specified source.
        /// </summary>
        /// <param name="newSource"></param>
        public abstract void ChangeActiveSource(PackageSource newSource);
    }
}

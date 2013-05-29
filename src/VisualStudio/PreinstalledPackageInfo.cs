using System;
using System.Diagnostics;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// The information that represents a single preinstalled package (already on disk).
    /// </summary>
    internal sealed class PreinstalledPackageInfo
    {
        /// <summary>
        /// Information for a single preinstalled package that will have its assembly references added.
        /// </summary>
        /// <param name="id">The package Id.</param>
        /// <param name="version">The package version.</param>
        public PreinstalledPackageInfo(string id, string version) :
            this(id, version, skipAssemblyReferences: false)
        {
        }

        /// <summary>
        /// Information for a single preinstalled package.
        /// </summary>
        /// <param name="id">The package Id.</param>
        /// <param name="version">The package version.</param>
        /// <param name="skipAssemblyReferences">A boolean indicating whether assembly references from the package should be skipped.</param>
        public PreinstalledPackageInfo(string id, string version, bool skipAssemblyReferences)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(id));
            Debug.Assert(!String.IsNullOrWhiteSpace(version));

            Id = id;
            Version = new SemanticVersion(version);
            SkipAssemblyReferences = skipAssemblyReferences;
        }

        public string Id { get; private set; }
        public SemanticVersion Version { get; private set; }
        public bool SkipAssemblyReferences { get; private set; }
    }
}
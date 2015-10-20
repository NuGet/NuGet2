using System;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    public interface IPackageRestoreManager
    {
        /// <summary>
        /// Occurs when it is detected that the packages are missing or restored for the current solution.
        /// </summary>
        event EventHandler<PackagesMissingStatusEventArgs> PackagesMissingStatusChanged;

        /// <summary>
        /// Checks the current solution if there is any package missing.
        /// </summary>
        /// <returns></returns>
        void CheckForMissingPackages();

        /// <summary>
        /// Restores the missing packages for the current solution.
        /// </summary>
        Task RestoreMissingPackages();
    }
}
using System;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    public interface IPackageRestoreManager
    {
        /// <summary>
        /// Gets a value indicating whether the current solution is configured for Package Restore mode.
        /// </summary>
        bool IsCurrentSolutionEnabledForRestore { get; }

        /// <summary>
        /// Configures the current solution for Package Restore mode.
        /// </summary>
        /// <param name="quietMode">if set to <c>true</c>, the method will not show any error message.</param>
        void EnableCurrentSolutionForRestore(bool quietMode);

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
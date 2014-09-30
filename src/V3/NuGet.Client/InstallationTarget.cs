using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.Versioning;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client
{
    /// <summary>
    /// Represents a target into which packages can be installed
    /// </summary>
    public abstract class InstallationTarget
    {
#if DEBUG
        // Helper list for debug builds only.
        private static readonly HashSet<Type> KnownFeatures = new HashSet<Type>()
        {
            typeof(PowerShellScriptExecutionFeature),
            typeof(NuGetCoreInstallationFeature)
        };
#endif

        private readonly Dictionary<Type, Func<object>> _featureFactories = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// Gets the name of the target in which packages will be installed (for example, the Project name when targetting a Project)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a boolean indicating if the installation target is active and available for installation (i.e. is it open).
        /// </summary>
        public abstract bool IsActive { get; }

        /// <summary>
        /// Gets a boolean indicating if the installation target is a solution target.
        /// </summary>
        public virtual bool IsSolution
        {
            get { return TargetProjects.Count() > 1; }
        }

        /// <summary>
        /// Gets a list of installed packages in all projects in the solution, including those NOT targetted by this installation target.
        /// </summary>
        public abstract IEnumerable<InstalledPackagesList> InstalledPackagesInAllProjects { get; }

        /// <summary>
        /// Gets a list of all projects targetted by this installation target.
        /// </summary>
        public abstract IEnumerable<TargetProject> TargetProjects { get; }

        /// <summary>
        /// Searches the installed packages list
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<JObject>> SearchInstalled(string searchTerm, int skip, int take, CancellationToken cancelToken);

        /// <summary>
        /// Gets the project with the specified name, if it exists, otherwise returns null.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public virtual TargetProject GetProject(string projectName)
        {
            return TargetProjects.FirstOrDefault(p => String.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retrieves an instance of the requested feature, throwing a <see cref="RequiredFeatureNotSupportedException"/>
        /// if the feature is not supported by this host.
        /// </summary>
        /// <typeparam name="T">The type defining the feature to retrieve</typeparam>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        /// <exception cref="RequiredFeatureNotSupportedException">The target does not support this feature.</exception>
        public virtual T GetRequiredFeature<T>()
        {
            var feature = TryGetFeature<T>();
            if (feature == null)
            {
                throw new RequiredFeatureNotSupportedException(typeof(T));
            }
            return feature;
        }

        /// <summary>
        /// Retrieves an instance of the requested feature, throwing a <see cref="RequiredFeatureNotSupportedException"/>
        /// if the feature is not supported by this host.
        /// </summary>
        /// <param name="featureType">The type defining the feature to retrieve</param>
        /// <returns>An instance of <paramref name="featureType"/>.</returns>
        /// <exception cref="RequiredFeatureNotSupportedException">The host does not support this feature.</exception>
        public virtual object GetRequiredFeature(Type featureType)
        {
            var feature = TryGetFeature(featureType);
            if (feature == null)
            {
                throw new RequiredFeatureNotSupportedException(featureType);
            }
            return feature;
        }

        /// <summary>
        /// Retrieves an instance of the requested feature, if one exists in this host.
        /// </summary>
        /// <typeparam name="T">The type defining the feature to retrieve</typeparam>
        /// <returns>An instance of <typeparamref name="T"/>, or null if no such feature exists.</returns>
        public virtual T TryGetFeature<T>() { return (T)TryGetFeature(typeof(T)); }

        /// <summary>
        /// Retrieves an instance of the requested feature, if one exists in this host.
        /// </summary>
        /// <param name="featureType">The type defining the feature to retrieve</param>
        /// <returns>An instance of <paramref name="featureType"/>, or null if no such feature exists.</returns>
        public virtual object TryGetFeature(Type featureType)
        {
            Func<object> factory;
            if (!_featureFactories.TryGetValue(featureType, out factory))
            {
                return null;
            }
            return factory();
        }

        protected virtual void AddFeature<T>(Func<T> factory) where T : class
        {
            // During development, there should NEVER be a feature type added that we don't know about :).
            Debug.Assert(
                KnownFeatures.Contains(typeof(T)), 
                "You tried to register a feature I'm not familiar with. This isn't generally a good thing...");

            _featureFactories.Add(typeof(T), factory);
        }
    }
}

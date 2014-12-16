using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Diagnostics;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.Versioning;
using NuGet.Client;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    /// <summary>
    /// Represents a target into which packages can be installed
    /// </summary>
    public abstract class InstallationTarget : IInstallationTarget
    {
#if DEBUG
        // Helper list for debug builds only.
        private static readonly HashSet<Type> KnownFeatures = new HashSet<Type>()
        {
            typeof(PowerShellScriptExecutor),
            typeof(IProjectManager),
            typeof(IPackageManager),
            typeof(IProjectSystem),
            typeof(IPackageCacheRepository),
            typeof(ISharedPackageRepository),
            typeof(SourceRepository),
            typeof(NuGetAwareProject)
        };
#endif

        private readonly Dictionary<Type, Func<object>> _featureFactories = new Dictionary<Type, Func<object>>();

        public abstract string Name
        {
            get;
        }

        public abstract bool IsAvailable
        {
            get;
        }

        public abstract bool IsSolution
        {
            get;
        }

        /// <summary>
        /// Gets a list of packages DIRECTLY installed in this target (does not include any packages installed into sub-targets, for example Projects in a Solution)
        /// </summary>
        public abstract InstalledPackagesList InstalledPackages
        {
            get;
        }

        /// <summary>
        /// Gets the solution related to this target. If the current target is a solution, returns itself, otherwise, returns the parent solution.
        /// </summary>
        /// <returns></returns>
        public abstract Solution OwnerSolution
        {
            get;
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
            NuGetTraceSources.InstallationTarget.Error(
                "missingfeature",
                "[{0}] Required feature '{1}' was requested but is not provided",
                Name,
                featureType.FullName);
            Debug.Assert(feature != null, "Required feature '" + featureType.FullName + "' not found for this installation target: " + Name);
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

        protected void AddFeature<T>(Func<T> factory) where T : class
        {
#if DEBUG
            // During development, there should NEVER be a feature type added that we don't know about :).
            Debug.Assert(
                KnownFeatures.Contains(typeof(T)),
                "You tried to register a feature ('" + typeof(T).FullName + "') I'm not familiar with. This isn't generally a good thing...");
#endif

            _featureFactories.Add(typeof(T), factory);
        }

        /// <summary>
        /// Gets the list of frameworks supported by this target. Also, the first one 
        /// is assumed to be the current target framework.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method may require computation")]
        public abstract IEnumerable<FrameworkName> GetSupportedFrameworks();

        /// <summary>
        /// Searches installed packages across this target and any sub-targets (Projects in a Solution, for example)
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<JObject>> SearchInstalled(SourceRepository source, string searchText, int skip, int take, CancellationToken cancelToken);

        /// <summary>
        /// Retrieves this target and all of it's sub-targets (Projects in a Solution, for example) in a single flat list.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method can be expensive")]
        public abstract IEnumerable<InstallationTarget> GetAllTargetsRecursively();


        public virtual void AddMetricsMetadata(JObject metricsRecord)
        {
           
        }
    }
}

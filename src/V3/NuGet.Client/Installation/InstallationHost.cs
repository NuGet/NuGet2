using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Installation
{
    /// <summary>
    /// Represents a hosting environment in which installation of packages can take place. The core engine
    /// will, on executing an action for a package, request features from the Host in order to perform the action.
    /// </summary>
    public abstract class InstallationHost
    {
        /// <summary>
        /// Retrieves an instance of the requested feature, throwing a <see cref="RequiredFeatureNotSupportedException"/>
        /// if the feature is not supported by this host.
        /// </summary>
        /// <typeparam name="T">The type defining the feature to retrieve</typeparam>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        /// <exception cref="RequiredFeatureNotSupportedException">The host does not support this feature.</exception>
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
        public abstract object TryGetFeature(Type featureType);
    }
}

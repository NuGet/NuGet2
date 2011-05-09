using System;
using System.Collections.Generic;
using System.Net;

namespace NuGet {
    /// <summary>
    /// This interface represents the basic interface that one needs to implement to
    /// be able to provide support to the consumer for when a proxy object is required.
    /// </summary>
    public interface IProxyFinder {
        /// <summary>
        /// Returns an IWebProxy object instance that represents a valid
        /// proxy object that should be used for communicating.
        /// </summary>
        /// <param name="uri">The given Uri to get a proxy for.</param>
        /// <returns></returns>
        IWebProxy GetProxy(Uri uri);
        /// <summary>
        /// Allows the consumer to provide a list of proxy finding strategies to use
        /// for locating of different IWebProxy instances.
        /// </summary>
        /// <param name="strategy"></param>
        void RegisterProxyStrategy(IProxyFinderStrategy strategy);
        /// <summary>
        /// Unregisters the specified proxy finding strategy from the proxy finder.
        /// </summary>
        /// <param name="strategy"></param>
        void UnregisterProxyStrategy(IProxyFinderStrategy strategy);
        /// <summary>
        /// Returns a list of already registered IProxyFinderStrategy instances that one can enumerate
        /// </summary>
        ICollection<IProxyFinderStrategy> RegisteredStrategies {
            get;
        }
    }
}
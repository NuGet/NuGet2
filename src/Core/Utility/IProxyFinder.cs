using System;
using System.Net;

namespace NuGet {
    /// <summary>
    /// This interface represents the basic interface that one needs to implement to
    /// be able to provide support to the consumer for when a proxy object is required.
    /// </summary>
    public interface IProxyFinder: ICredentialProviderRegistry {
        /// <summary>
        /// Returns an IWebProxy object instance that represents a valid
        /// proxy object that should be used for communicating.
        /// </summary>
        /// <param name="uri">The given Uri to get a proxy for.</param>
        /// <returns></returns>
        IWebProxy GetProxy(Uri uri);
    }
}
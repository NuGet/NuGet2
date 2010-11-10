using System;
using System.Diagnostics;

namespace NuGet.Runtime {
    public static class AppDomainExtensions {
        /// <summary>
        /// Creates an instance of a type in another application domain
        /// </summary>
        public static T CreateInstance<T>(this AppDomain domain) {
            // T must extend MarshalByRefObject in order to be instantiated in another app domain
            Debug.Assert(typeof(MarshalByRefObject).IsAssignableFrom(typeof(T)));

            return (T)domain.CreateInstanceAndUnwrap(typeof(T).Assembly.FullName,
                                                     typeof(T).FullName);
        }
    }
}

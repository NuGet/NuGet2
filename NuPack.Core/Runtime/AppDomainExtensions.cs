using System;
using System.Diagnostics;

namespace NuGet.Runtime {
    public static class AppDomainExtensions {
        public static T CreateInstance<T>(this AppDomain domain) {
            Debug.Assert(typeof(MarshalByRefObject).IsAssignableFrom(typeof(T)));
            return (T)domain.CreateInstanceAndUnwrap(typeof(T).Assembly.FullName,
                                                     typeof(T).FullName);
        }
    }
}

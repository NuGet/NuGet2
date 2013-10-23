// -----------------------------------------------------------------------
// <copyright file="WebPagesDeployment.cs" company="Microsoft">
// A reflection wrapper class for methods in the WebPagesDeployment class.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// A reflection wrapper class for methods in the WebPagesDeployment class.
    /// </summary>
    internal static class WebPagesDeployment
    {
        private const string AssemblyName = "System.Web.WebPages.Deployment, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
        private const string TypeName = "System.Web.WebPages.Deployment.WebPagesDeployment";

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                // perform initialization first on a reflection-loaded assemly
                // this way if we fail we can always try again later
                var reflectionAssembly = Assembly.ReflectionOnlyLoad(AssemblyName);
                InitializeMethods(reflectionAssembly);

                var realAssembly = Assembly.Load(AssemblyName);
                InitializeMethods(realAssembly);

                IsInitialized = true;
            }
        }

        private static void InitializeMethods(Assembly assembly)
        {
            Debug.Assert(assembly != null, "Assembly must not be null");
            Type type = assembly.GetType(TypeName, throwOnError: true);

            GetIncompatibleDependenciesMethod = GetStaticMethod(type, "GetIncompatibleDependencies");
            GetMaxVersionMethod = GetStaticMethod(type, "GetMaxVersion");
            GetExplicitWebPagesVersionMethod = GetStaticMethod(type, "GetExplicitWebPagesVersion");
            GetWebPagesAssembliesMethod = GetStaticMethod(type, "GetWebPagesAssemblies");
            IsEnabledMethod = GetStaticMethod(type, "IsEnabled");
        }

        private static MethodInfo GetStaticMethod(Type type, string name)
        {
            Debug.Assert(type != null, "Type must not be null");
            Debug.Assert(name != null, "Name must not be null");

            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                throw new MissingMethodException(type.Name, name);
            }
            else
            {
                return method;
            }
        }

        private static bool IsInitialized
        {
            get;
            set;
        }

        public static IDictionary<string, Version> GetIncompatibleDependencies(string physicalPath)
        {
            EnsureInitialized();
            try
            {
                return InvokeStaticAndUnwrap<IDictionary<string, Version>>(GetIncompatibleDependenciesMethod, new object[] { physicalPath });
            }
            catch
            {
                // this will fail sometimes, like when the 'bin' directory doesn't exist
                // or if the web config is invalid. When this bin directory doesn't exist,
                // it throws ArgumentNullException -- which is a wierd exception to handle.
                // we have a bug tracking this
                return new Dictionary<string, Version>();
            }
        }

        private static MethodInfo GetIncompatibleDependenciesMethod
        {
            get;
            set;
        }

        public static Version GetMaxVersion()
        {
            EnsureInitialized();
            return InvokeStaticAndUnwrap<Version>(GetMaxVersionMethod, null);
        }

        private static MethodInfo GetMaxVersionMethod
        {
            get;
            set;
        }

        public static Version GetExplicitWebPagesVersion(string physicalPath)
        {
            EnsureInitialized();
            return InvokeStaticAndUnwrap<Version>(GetExplicitWebPagesVersionMethod, new object[] { physicalPath });
        }

        private static MethodInfo GetExplicitWebPagesVersionMethod
        {
            get;
            set;
        }

        public static IEnumerable<AssemblyName> GetWebPagesAssemblies()
        {
            EnsureInitialized();
            return InvokeStaticAndUnwrap<IEnumerable<AssemblyName>>(GetWebPagesAssembliesMethod, null);
        }

        private static MethodInfo GetWebPagesAssembliesMethod
        {
            get;
            set;
        }

        public static bool IsEnabled(string physicalPath)
        {
            EnsureInitialized();
            return InvokeStaticAndUnwrap<bool>(IsEnabledMethod, new object[] { physicalPath });
        }

        private static MethodInfo IsEnabledMethod
        {
            get;
            set;
        }

        private static T InvokeStaticAndUnwrap<T>(MethodInfo method, object[] parameters)
        {
            Debug.Assert(method != null, "Method should not be null");
            Debug.Assert(method.IsStatic, "Method must be static");

            try
            {
                return (T)method.Invoke(null, parameters);
            }
            catch (TargetInvocationException tie)
            {
                // unwrap to simulate a 'real' call
                throw tie.InnerException;
            }
        }
    }
}

using System;
using System.Reflection;

namespace NuGet {
    internal static class AppDomainUtils {
        private const string NuGetCoreResourceName = "NuGet.NuGet.Core.dll";
        private const string AppDomainFriendlyName = "CommandLineAppDomain";

        public static Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args) {
            if (args.Name.StartsWith("NuGet.Core", StringComparison.OrdinalIgnoreCase)) {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(NuGetCoreResourceName)) {
                    byte[] assemblyContent = new byte[stream.Length];
                    stream.Read(assemblyContent, 0, assemblyContent.Length);
                    return Assembly.Load(assemblyContent);
                }
            }
            return null;
        }

        /// <summary>
        /// Creates an AppDomain with the application base set to the current app domain's base directory and assembly resolve event handlers wired up correctly.
        /// </summary>
        public static AppDomain CloneAppDomain() {
            var setup = new AppDomainSetup {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            };

            AppDomain domain = AppDomain.CreateDomain(AppDomainFriendlyName, AppDomain.CurrentDomain.Evidence, setup);
            domain.AssemblyResolve += AssemblyResolveEventHandler;

            return domain;
        }
    }
}

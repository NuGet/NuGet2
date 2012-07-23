using NuGet.ExecutionModel;
using NuGet.VisualStudio.ExecutionModel;
using System;

namespace NuGet.VisualStudio
{
    public static class CodeInvoker
    {
        public static void InvokeInit(string assemblyPath, string rootPath, IPackage package)
        {
            var installContext = new InstallContext(rootPath, package, null);
            ExecuteRemotely(worker => worker.OnInit(assemblyPath, installContext, NullLogger.Instance));
        }

        public static void InvokeInstall(string assemblyPath, string rootPath, IPackage package, IProjectSystem project)
        {
            var installContext = new InstallContext(rootPath, package, new VsProjectProxy(project));
            ExecuteRemotely(worker => worker.OnInstall(assemblyPath, installContext, NullLogger.Instance));
        }

        public static void InvokeUninstall(string assemblyPath, string rootPath, IPackage package, IProjectSystem project)
        {
            var uninstallContext = new UninstallContext(rootPath, package, new VsProjectProxy(project));
            ExecuteRemotely(worker => worker.OnUninstall(assemblyPath, uninstallContext, NullLogger.Instance));
        }

        private static void ExecuteRemotely(Action<AppDomainWorker> workerAction)
        {
            AppDomain domain = AppDomain.CreateDomain("CodeInvoker");
            try
            {
                // load NuGet.Core.dll into the other app domain
                domain.Load(typeof(IPackage).Assembly.GetName());

                var worker = (AppDomainWorker)domain.CreateInstanceFromAndUnwrap(
                    "NuGet.Core.dll", "NuGet.ExecutionModel.AppDomainWorker");

                workerAction(worker);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }
}
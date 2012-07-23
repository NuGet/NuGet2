using System;
using System.ComponentModel;
using System.Reflection;

namespace NuGet.ExecutionModel
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AppDomainWorker : MarshalByRefObject
    {
        public void OnInit(string assemblyPath, InstallContext installContext, ILogger logger)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            ExecuteCode(assemblyPath, "OnInit", installContext, logger);
        }

        public void OnInstall(string assemblyPath, InstallContext installContext, ILogger logger)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            ExecuteCode(assemblyPath, "OnInstall", installContext, logger);
        }

        public void OnUninstall(string assemblyPath, UninstallContext uninstallContext, ILogger logger)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            ExecuteCode(assemblyPath, "OnUninstall", uninstallContext, logger);
        }
        
        private void ExecuteCode(string assemblyPath, string methodName, object parameter, ILogger logger)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            Type type = assembly.GetType("NuGet.Code");
            if (type != null)
            {
                var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (methodInfo != null)
                {
                    try
                    {
                        methodInfo.Invoke(null, new object[] { parameter });
                        logger.Log(MessageLevel.Info, "Method executed succcessfully.");
                    }
                    catch (Exception exception)
                    {
                        logger.Log(MessageLevel.Error, exception.GetBaseException().Message);
                    }
                }
            }
        }
    }
}
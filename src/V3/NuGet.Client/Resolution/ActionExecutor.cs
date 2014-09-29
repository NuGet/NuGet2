using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Interop;
using NuGet.Resolver;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;
using OldPackageAction = NuGet.Resolver.PackageAction;
using NuGet.Client.Diagnostics;
using NuGet.Client.Installation;

namespace NuGet.Client.Resolution
{
    public interface IActionHandler
    {
        Task Execute(NewPackageAction action, InstallationHost host, IExecutionLogger logger);
        Task Rollback(NewPackageAction action, InstallationHost host, IExecutionLogger logger);
    }

    public class ActionExecutor
    {
        private static readonly Dictionary<PackageActionType, IActionHandler> _actionHandlers = new Dictionary<PackageActionType, IActionHandler>()
        {
            { PackageActionType.Download, new DownloadActionHandler() },
            { PackageActionType.Install, new InstallActionHandler() },
            { PackageActionType.Uninstall, new UninstallActionHandler() },
            { PackageActionType.Purge, new PurgeActionHandler() },
        };

        public virtual Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions, InstallationHost host)
        {
            return ExecuteActionsAsync(actions, host, NullExecutionLogger.Instance);
        }

        public virtual async Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions, InstallationHost host, IExecutionLogger logger)
        {
            // Capture actions we've already done so we can roll them back in case of an error
            var executedActions = new List<NewPackageAction>();
            try
            {
                foreach (var action in actions)
                {
                    IActionHandler handler;
                    if (!_actionHandlers.TryGetValue(action.ActionType, out handler))
                    {
                        NuGetTraceSources.ActionExecutor.Error(
                            "execute/unhandledaction",
                            "[{0}] Skipping unknown action: {1}",
                            action.PackageIdentity,
                            action.ToString());
                    }
                    else
                    {
                        NuGetTraceSources.ActionExecutor.Info(
                            "execute/executing",
                            "[{0}] Executing action: {1}",
                            action.PackageIdentity,
                            action.ToString());
                        await handler.Execute(action, host, logger);
                        executedActions.Add(action);
                    }
                }
            }
            catch
            {
                // Roll back the actions and rethrow
                Rollback(executedActions, host, logger);
                throw;
            }
        }

        protected virtual void Rollback(ICollection<NewPackageAction> executedActions, InstallationHost host, IExecutionLogger logger)
        {
            if (executedActions.Count > 0)
            {
                // Only print the rollback warning if we have something to rollback
                logger.Log(MessageLevel.Warning, Strings.ActionExecutor_RollingBack);
            }

            foreach (var action in executedActions.Reverse())
            {
                IActionHandler handler;
                if (!_actionHandlers.TryGetValue(action.ActionType, out handler))
                {
                    NuGetTraceSources.ActionExecutor.Error(
                        "rollback/unhandledaction",
                        "[{0}] Skipping unknown action: {1}",
                        action.PackageIdentity,
                        action.ToString());
                }
                else
                {
                    NuGetTraceSources.ActionExecutor.Info(
                        "rollback/executing",
                        "[{0}] Executing action: {1}",
                        action.PackageIdentity,
                        action.ToString());
                    handler.Rollback(action, host, logger).Wait();
                }
            }
        }
    }
}

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

namespace NuGet.Client.Resolution
{
    public interface IActionHandler
    {
        Task Execute(NewPackageAction action, ExecutionContext context, IExecutionLogger logger);
        Task Rollback(NewPackageAction action, ExecutionContext context, IExecutionLogger logger);
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

        public virtual Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions, ExecutionContext context)
        {
            return ExecuteActionsAsync(actions, context, NullExecutionLogger.Instance);
        }

        public virtual async Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions, ExecutionContext context, IExecutionLogger logger)
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
                            action.PackageName,
                            action.ToString());
                    }
                    else
                    {
                        NuGetTraceSources.ActionExecutor.Info(
                            "execute/executing",
                            "[{0}] Executing action: {1}",
                            action.PackageName,
                            action.ToString());
                        await handler.Execute(action, context, logger);
                        executedActions.Add(action);
                    }
                }
            }
            catch
            {
                // Roll back the actions and rethrow
                Rollback(executedActions, context, logger);
                throw;
            }
        }

        protected virtual void Rollback(ICollection<NewPackageAction> executedActions, ExecutionContext context, IExecutionLogger logger)
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
                        action.PackageName,
                        action.ToString());
                }
                else
                {
                    NuGetTraceSources.ActionExecutor.Info(
                        "rollback/executing",
                        "[{0}] Executing action: {1}",
                        action.PackageName,
                        action.ToString());
                    handler.Rollback(action, context, logger).Wait();
                }
            }
        }
    }
}

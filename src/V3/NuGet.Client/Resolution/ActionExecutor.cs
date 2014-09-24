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
        Task Execute(NewPackageAction action, ExecutionContext context);
        Task Rollback(NewPackageAction action, ExecutionContext context);
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

        public virtual async Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions, ExecutionContext context)
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
                            "unhandledaction",
                            "[{0}] Skipping unknown action: {1}",
                            action.PackageName,
                            action.ToString());
                    }
                    else
                    {
                        NuGetTraceSources.ActionExecutor.Info(
                            "executing",
                            "[{0}] Executing action: {1}",
                            action.PackageName,
                            action.ToString());
                        await handler.Execute(action, context);
                        executedActions.Add(action);
                    }
                }
            }
            catch
            {
                // Roll back the actions and rethrow
                Rollback(executedActions, context);
                throw;
            }
        }

        protected virtual void Rollback(List<NewPackageAction> executedActions, ExecutionContext context)
        {
            foreach (var action in executedActions)
            {
                NuGetTraceSources.ActionExecutor.Error(
                                "rollback_notimplemented",
                                "[{0}] Rollback of {1} not yet implemented.",
                                action.PackageName,
                                action.ActionType.ToString());
            }
        }
    }
}

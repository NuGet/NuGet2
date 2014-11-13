using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using NuGet.Client.Diagnostics;
using NuGet.Client.Resolution;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client.Installation
{
    public interface IActionHandler
    {
        //!!! TODO: remove cancelToken
        void Execute(NewPackageAction action, IExecutionContext context, CancellationToken cancelToken);

        void Rollback(NewPackageAction action, IExecutionContext context); // Rollbacks should not be cancelled, it's a Bad Idea(TM)
    }

    // TODO: need to consider which actions can be executed async and how.
    public class ActionExecutor
    {
        private static readonly Dictionary<PackageActionType, IActionHandler> _actionHandlers = new Dictionary<PackageActionType, IActionHandler>()
        {
            { PackageActionType.Download, new DownloadActionHandler() },
            { PackageActionType.Install, new InstallActionHandler() },
            { PackageActionType.Uninstall, new UninstallActionHandler() },
            { PackageActionType.Purge, new PurgeActionHandler() },
        };

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "By defintion we want to catch all exceptions")]
        public void ExecuteActions(IEnumerable<NewPackageAction> actions, IExecutionContext context)
        {
            // Capture actions we've already done so we can roll them back in case of an error
            var executedActions = new List<NewPackageAction>();

            ExceptionDispatchInfo capturedException = null;
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
                        handler.Execute(action, context, CancellationToken.None);
                        executedActions.Add(action);
                    }
                }
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }

            if (capturedException != null)
            {
                // Roll back the actions and rethrow
                Rollback(executedActions, context);
                capturedException.Throw();
            }
        }

        protected virtual void Rollback(ICollection<NewPackageAction> executedActions, IExecutionContext context)
        {
            if (executedActions.Count > 0)
            {
                // Only print the rollback warning if we have something to rollback
                context.Log(MessageLevel.Warning, Strings.ActionExecutor_RollingBack);
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
                    handler.Rollback(action, context);
                }
            }
        }
    }
}
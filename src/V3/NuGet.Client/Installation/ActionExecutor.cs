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

    public class UserAction
    {
        public UserAction(PackageActionType action, PackageIdentity package)
        {
            Action = action;
            PackageIdentity = package;
        }

        public PackageActionType Action { get; private set; }

        public PackageIdentity PackageIdentity { get; private set; }
    }

    // TODO: need to consider which actions can be executed async and how.
    public class ActionExecutor
    {
        public static readonly string ReadmeFileName = "readme.txt";

        private UserAction _userAction;
        private string _readmeFilePath;

        private static readonly Dictionary<PackageActionType, IActionHandler> _actionHandlers = new Dictionary<PackageActionType, IActionHandler>()
        {
            { PackageActionType.Download, new DownloadActionHandler() },
            { PackageActionType.Install, new InstallActionHandler() },
            { PackageActionType.Uninstall, new UninstallActionHandler() },
            { PackageActionType.Purge, new PurgeActionHandler() },
        };

        /// <summary>
        /// Executes actions in response to the user action.
        /// </summary>
        /// <param name="actions">The actions to execute.</param>
        /// <param name="context">The context in which to execute the actions.</param>
        /// <param name="userAction">The user action from which <paramref name="actions"/> are resolved.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "By defintion we want to catch all exceptions")]
        public void ExecuteActions(
            IEnumerable<NewPackageAction> actions,
            IExecutionContext context,
            UserAction userAction = null)
        {
            // Capture actions we've already done so we can roll them back in case of an error
            var executedActions = new List<NewPackageAction>();

            _userAction = userAction;
            ExceptionDispatchInfo capturedException = null;
            try
            {
                _readmeFilePath = null;

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

                    UpdateReadmeFilePath(action);
                }

                if (_readmeFilePath != null)
                {
                    context.OpenFile(_readmeFilePath);
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

        private void UpdateReadmeFilePath(NewPackageAction action)
        {
            if (_userAction == null || _userAction.Action != PackageActionType.Install)
            {
                // display readme file only when installing packages
                return;
            }

            // look for readme file
            if (action.PackageIdentity.Equals(_userAction.PackageIdentity) &&
                action.ActionType == PackageActionType.Install)
            {
                _readmeFilePath = GetReadmeFilePath(action);
            }
        }

        private static string GetReadmeFilePath(NewPackageAction action)
        {
            // Get the package manager and project manager from the target
            var packageManager = action.Target.TryGetFeature<IPackageManager>();
            if (packageManager == null)
            {
                return null;
            }

            // Get the package from the shared repository
            var package = packageManager.LocalRepository.FindPackage(
                action.PackageIdentity.Id, CoreConverters.SafeToSemVer(action.PackageIdentity.Version));

            if (package != null &&
                package.GetFiles().Any(f => f.Path.Equals(ReadmeFileName, StringComparison.OrdinalIgnoreCase)))
            {
                var packageInstalledPath = packageManager.PathResolver.GetInstallPath(package);
                return System.IO.Path.Combine(
                    packageInstalledPath,
                    ReadmeFileName);
            }

            return null;
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
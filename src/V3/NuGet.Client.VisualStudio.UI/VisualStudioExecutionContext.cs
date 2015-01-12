using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using NuGet.VisualStudio;
using NuGetConsole;

namespace NuGet.Client.VisualStudio.UI
{
    class VisualStudioExecutionContext : IExecutionContext
    {
        private readonly Dispatcher _uiDispatcher;
        private IConsole _outputConsole;

        public VisualStudioExecutionContext(IConsole outputConsole)
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            _outputConsole = outputConsole;
        }

        public void ExecuteScript(string packageInstallPath, string scriptRelativePath, object package, Installation.InstallationTarget target)
        {
            var executor = new VsPowerShellScriptExecutor(ServiceLocator.GetInstance<IScriptExecutor>());
            executor.ExecuteScript(packageInstallPath, scriptRelativePath, package, target, this);
        }

        public void OpenFile(string fullPath)
        {
            var commonOperations = ServiceLocator.GetInstance<IVsCommonOperations>();
            commonOperations.OpenFile(fullPath);
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            var s = string.Format(CultureInfo.CurrentCulture, message, args);
            _outputConsole.WriteLine(s);
        }

        public FileConflictAction FileConflictAction
        {
            get;
            set;
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            if (FileConflictAction == FileConflictAction.PromptUser)
            {
                var resolution = ShowFileConflictResolution(message);

                if (resolution == FileConflictAction.IgnoreAll ||
                    resolution == FileConflictAction.OverwriteAll)
                {
                    FileConflictAction = resolution;
                }
                return resolution;
            }

            return FileConflictAction;
        }

        private FileConflictAction ShowFileConflictResolution(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Func<string, FileConflictAction>(ShowFileConflictResolution),
                    message);
                return (FileConflictAction)result;
            }

            var fileConflictDialog = new FileConflictDialog()
            {
                Question = message
            };

            if (fileConflictDialog.ShowModal() == true)
            {
                return fileConflictDialog.UserSelection;
            }
            else
            {
                return FileConflictAction.IgnoreAll;
            }
        }
    }
}

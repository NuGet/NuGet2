using System.Diagnostics;
using NuGet.Client.Installation;

namespace NuGet.Client
{
    public enum MessageLevel
    {
        Info,
        Warning,
        Debug,
        Error
    }

    public interface IExecutionLogger
    {
        void Log(MessageLevel level, string message, params object[] args);

        FileConflictAction ResolveFileConflict(string message);
    }

    public class NullExecutionLogger : IExecutionLogger
    {
        private static readonly IExecutionLogger _instance = new NullExecutionLogger();

        public static IExecutionLogger Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return FileConflictAction.IgnoreAll;
        }
    }

    public class ShimLogger : ILogger
    {
        private IExecutionLogger _logger;

        public ShimLogger(IExecutionLogger logger)
        {
            _logger = logger;
        }

        public void Log(NuGet.MessageLevel level, string message, params object[] args)
        {
            _logger.Log((MessageLevel)(int)level, message, args);
        }

        public NuGet.FileConflictResolution ResolveFileConflict(string message)
        {
            var action = _logger.ResolveFileConflict(message);
            Debug.Assert(action != FileConflictAction.PromptUser);

            if (action == FileConflictAction.Overwrite ||
                action == FileConflictAction.OverwriteAll)
            {
                return FileConflictResolution.Overwrite;
            }
            else
            {
                return FileConflictResolution.Ignore;
            }
        }
    }

    // Abstraction of the environment where actions are executed. It can be
    // nuget.exe, powershell, or UI.
    public interface IExecutionContext : IExecutionLogger
    {
        /// <summary>
        /// Execute powershell script in the package
        /// </summary>
        /// <param name="packageInstallPath">The full root path of the installed package. E.g.
        /// c:\temp\packages\jquery2.1.1</param>
        /// <param name="scriptRelativePath">The path of the script file relative to <paramref name="packageInstallPath"/>.
        /// E.g. tools\init.ps1</param>
        /// <param name="package">The package. The type of parameter package is object because we don't want
        /// to expose IPackage.</param>
        /// <param name="target">The target.</param>
        void ExecuteScript(string packageInstallPath, string scriptRelativePath, object package, InstallationTarget target);

        void OpenFile(string fullPath);
    }

    public class NullExecutionContext : IExecutionContext
    {
        private static readonly NullExecutionContext _instance = new NullExecutionContext();

        public static IExecutionContext Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            // no-op
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return FileConflictAction.IgnoreAll;
        }

        public void ExecuteScript(string packageInstallPath, string scriptRelativePath, object package, InstallationTarget target)
        {
            // no-op
        }

        public void OpenFile(string fullPath)
        {
            // no-op
        }
    }
}
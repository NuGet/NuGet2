using NuGet.Common;

namespace NuGet.Commands
{
    public class UpdateCommandLogger
        : ILogger
    {
        private readonly IConsole _console;
        private readonly FileConflictAction _fileConflictAction;
        private readonly bool _verbose;
        private bool _overwriteAll, _ignoreAll;

        public UpdateCommandLogger(IConsole console, FileConflictAction fileConflictAction, bool verbose)
        {
            _console = console;
            _fileConflictAction = fileConflictAction;
            _verbose = verbose;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            if (_verbose && _console != null)
            {
                _console.Log(level, message, args);
            }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            // the -FileConflictAction is set to Overwrite or user has chosen Overwrite All previously
            if (_fileConflictAction == FileConflictAction.Overwrite || _overwriteAll)
            {
                return FileConflictResolution.Overwrite;
            }

            // the -FileConflictAction is set to Ignore or user has chosen Ignore All previously
            if (_fileConflictAction == FileConflictAction.Ignore || _ignoreAll)
            {
                return FileConflictResolution.Ignore;
            }

            // otherwise, prompt user for choice, unless we're in non-interactive mode
            if (_console != null && !_console.IsNonInteractive)
            {
                var resolution = _console.ResolveFileConflict(message);
                _overwriteAll = (resolution == FileConflictResolution.OverwriteAll);
                _ignoreAll = (resolution == FileConflictResolution.IgnoreAll);
                return resolution;
            }

            return FileConflictResolution.Ignore;
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using NuGet.VisualStudio.Resources;

namespace NuGet.PowerShell.Commands
{
    public class PowerShellCmdletLogger
        : ILogger
    {
        private readonly FileConflictAction? _fileConflictAction;
        private readonly PSCmdlet _cmdlet;
        private readonly IErrorHandler _errorHandler;
        private bool _overwriteAll, _ignoreAll;

        public PowerShellCmdletLogger(PSCmdlet cmdlet, IErrorHandler errorHandler)
            : this(cmdlet, errorHandler, null)
        {
        }

        public PowerShellCmdletLogger(PSCmdlet cmdlet, IErrorHandler errorHandler, FileConflictAction? fileConflictAction)
        {
            _cmdlet = cmdlet;
            _errorHandler = errorHandler;
            _fileConflictAction = fileConflictAction;
        }
        
        public virtual FileConflictResolution ResolveFileConflict(string message)
        {
            if (_fileConflictAction != null)
            {
                if (_fileConflictAction == FileConflictAction.Overwrite)
                {
                    return FileConflictResolution.Overwrite;
                }

                if (_fileConflictAction == FileConflictAction.Ignore)
                {
                    return FileConflictResolution.Ignore;
                }
            }

            if (_overwriteAll)
            {
                return FileConflictResolution.OverwriteAll;
            }

            if (_ignoreAll)
            {
                return FileConflictResolution.IgnoreAll;
            }

            var choices = new Collection<ChoiceDescription>
            {
                new ChoiceDescription(Resources.Cmdlet_Yes, Resources.Cmdlet_FileConflictYesHelp),
                new ChoiceDescription(Resources.Cmdlet_YesAll, Resources.Cmdlet_FileConflictYesAllHelp),
                new ChoiceDescription(Resources.Cmdlet_No, Resources.Cmdlet_FileConflictNoHelp),
                new ChoiceDescription(Resources.Cmdlet_NoAll, Resources.Cmdlet_FileConflictNoAllHelp)
            };

            var choice = _cmdlet.Host.UI.PromptForChoice(VsResources.FileConflictTitle, message, choices, defaultChoice: 2);

            Debug.Assert(choice >= 0 && choice < 4);
            switch (choice)
            {
                case 0:
                    return FileConflictResolution.Overwrite;

                case 1:
                    _overwriteAll = true;
                    return FileConflictResolution.OverwriteAll;

                case 2:
                    return FileConflictResolution.Ignore;

                case 3:
                    _ignoreAll = true;
                    return FileConflictResolution.IgnoreAll;
            }

            return FileConflictResolution.Ignore;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            var formattedMessage = message;
            if (args != null)
            {
                formattedMessage = string.Format(CultureInfo.CurrentCulture, message, args);
            }

            switch (level)
            {
                case MessageLevel.Debug:
                    _cmdlet.WriteVerbose(formattedMessage);
                    break;

                case MessageLevel.Warning:
                    _cmdlet.WriteWarning(formattedMessage);
                    break;

                case MessageLevel.Info:
                    WriteLine(formattedMessage);
                    break;

                case MessageLevel.Error:
                    WriteError(formattedMessage);
                    break;
            }
        }

        private void WriteLine(string message = null)
        {
            if (_cmdlet.Host == null)
            {
                // Host is null when running unit tests. Simply return in this case
                return;
            }

            if (message == null)
            {
                _cmdlet.Host.UI.WriteLine();
            }
            else
            {
                _cmdlet.Host.UI.WriteLine(message);
            }
        }
        
        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes",
            Justification = "This exception is passed to PowerShell. We really don't care about the type of exception here.")]
        private void WriteError(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                WriteError(new Exception(message));
            }
        }

        private void WriteError(Exception exception)
        {
            _errorHandler.HandleException(exception, terminating: false);
        }
    }
}
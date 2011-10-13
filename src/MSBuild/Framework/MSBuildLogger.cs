using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NuGet.MSBuild
{
    public class MSBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper _buildLog;

        public MSBuildLogger(TaskLoggingHelper buildLog)
        {
            _buildLog = buildLog;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Info:
                    _buildLog.LogMessage(MessageImportance.Normal, message, args);
                    break;
                case MessageLevel.Warning:
                    _buildLog.LogMessage(MessageImportance.High, message, args);
                    break;
                case MessageLevel.Debug:
                    _buildLog.LogMessage(MessageImportance.Low, message, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}
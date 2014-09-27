using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resolution
{
    public enum FileConflictResolution
    {
        Overwrite,
        Ignore,
        OverwriteAll,
        IgnoreAll
    }

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

        FileConflictResolution ResolveFileConflict(string message);
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


        public FileConflictResolution ResolveFileConflict(string message)
        {
            return FileConflictResolution.Ignore;
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
            return (NuGet.FileConflictResolution)(int)_logger.ResolveFileConflict(message);
        }
    }
}

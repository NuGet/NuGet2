using System;

namespace NuGet
{
    public class NullLogger : MarshalByRefObject, ILogger
    {
        private static readonly ILogger _instance = new NullLogger();

        public static ILogger Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
        }
    }
}

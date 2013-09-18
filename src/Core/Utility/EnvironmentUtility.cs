using System;

namespace NuGet
{
    public static class EnvironmentUtility
    {
        private static bool _runningFromCommandLine;
        private static readonly bool _isMonoRuntime = Type.GetType("Mono.Runtime") != null;

        public static bool IsMonoRuntime
        {
            get
            {
                return _isMonoRuntime;
            }
        }

        public static bool RunningFromCommandLine
        {
            get
            {
                return _runningFromCommandLine;
            }
        }

        // this will be called from nuget.exe
        public static void SetRunningFromCommandLine()
        {
            _runningFromCommandLine = true;
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;

namespace NuGet.Test.Integration
{
    public class CommandRunner
    {
        public static Tuple<int, string> Run(string process, string workingDirectory, string arguments, bool waitForExit, int timeOutInMilliseconds = 60000)
        {
            string result = String.Empty;

            ProcessStartInfo psi = new ProcessStartInfo(Path.GetFullPath(process), arguments)
            {
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            StreamReader standardOutput;
            StreamReader errorOutput;
            int exitCode = 1;

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();
                standardOutput = p.StandardOutput;
                errorOutput = p.StandardError;

                if (waitForExit)
                {
                    bool processExited = p.WaitForExit(timeOutInMilliseconds);
                    if (!processExited)
                    {
                        p.Kill();
                    }
                }

                result = standardOutput.ReadToEnd();
                if (string.IsNullOrEmpty(result))
                {
                    result = errorOutput.ReadToEnd();
                }
                
                if (p.HasExited)
                {
                    exitCode = p.ExitCode;
                }
            }           

            Console.WriteLine(result);

            return Tuple.Create<int, string>(exitCode, result);
        }
    }
}

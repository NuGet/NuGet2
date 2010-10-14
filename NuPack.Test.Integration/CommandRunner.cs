namespace NuPack.Test.Integration {

    using System;
    using System.Diagnostics;
    using System.IO;

    public class CommandRunner {

        public static string Run(string process, string workingDirectory, string arguments, bool waitForExit) {

            string result = string.Empty;

            ProcessStartInfo psi = new ProcessStartInfo(Path.GetFullPath(process), arguments) {
                                           WorkingDirectory = Path.GetFullPath(workingDirectory),
                                           UseShellExecute = false,
                                           CreateNoWindow = true,
                                           RedirectStandardOutput = true,
                                           RedirectStandardError = true,
                                       };

            StreamReader standardOutput;
            StreamReader errorOutput;


            using (Process p = new Process()) {
                p.StartInfo = psi;
                p.Start();
                standardOutput = p.StandardOutput;
                errorOutput = p.StandardError;

                if (waitForExit) {
                    p.WaitForExit(1000);
                }
            }
            result = standardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(result)) {
                result = errorOutput.ReadToEnd();
            }

            Console.WriteLine(result);

            return result;
        }
    }
}
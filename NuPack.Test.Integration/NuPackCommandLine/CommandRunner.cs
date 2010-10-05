namespace NuPack.Test.Integration.NuPackCommandLine {

    using System;
    using System.Diagnostics;
    using System.IO;

    public class CommandRunner {

        public static string Run(string process, string working_directory, string arguments, bool wait_for_exit) {

            string result = string.Empty;

            ProcessStartInfo psi = new ProcessStartInfo(Path.GetFullPath(process), arguments) {
                                           WorkingDirectory = Path.GetFullPath(working_directory),
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

                if (wait_for_exit) {
                    p.WaitForExit();
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
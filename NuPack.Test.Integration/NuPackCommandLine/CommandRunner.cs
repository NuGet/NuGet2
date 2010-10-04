namespace NuPack.Test.NuPackCommandLine
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class CommandRunner
    {
        public static string Run(string process, string working_directory, string arguments, bool wait_for_exit)
        {
            string result = string.Empty;


            ProcessStartInfo psi = new ProcessStartInfo(Path.GetFullPath(process), arguments)
                                       {
                                           WorkingDirectory = Path.GetFullPath(working_directory),
                                           UseShellExecute = false,
                                           CreateNoWindow = true,
                                           RedirectStandardOutput = true,
                                           RedirectStandardError = true,

                                       };

            StreamReader standard_output;
            StreamReader error_output;


            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();
                standard_output = p.StandardOutput;
                error_output = p.StandardError;

                if (wait_for_exit)
                {
                    p.WaitForExit();
                }
            }
            result = standard_output.ReadToEnd();
            if (string.IsNullOrEmpty(result))
            {
                result = error_output.ReadToEnd();
            }

            Console.WriteLine(result);

            return result;
        }
    }
}
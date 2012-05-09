using System;
using System.Diagnostics;
using System.IO;
using NuGet;

namespace Bootstrapper
{
    public class Program
    {
        public static int Main(string[] args)
        {
            string _exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NuGet\NuGet.exe");
            try
            {
                var processInfo = new ProcessStartInfo(_exePath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                if (!File.Exists(_exePath))
                {
                    // Register a console based credentials provider so that the user get's prompted if a password
                    // is required for the proxy
                    HttpClient.DefaultCredentialProvider = new ConsoleCredentialProvider();

                    // Setup IHttpClient for the Gallery to locate packages
                    var httpClient = new HttpClient(new Uri("http://nuget.org/NuGet.exe"));
                    httpClient.UserAgent = "Bootstrapper/" + typeof(Program).Assembly.GetName().Version;

                    File.WriteAllBytes(_exePath, httpClient.DownloadData());
                }
                else if ((DateTime.UtcNow - File.GetLastWriteTime(_exePath)).TotalDays > 10)
                {
                    // Check for updates to the exe every 10 days
                    processInfo.Arguments = "update -self";
                    RunProcess(processInfo);
                    File.SetLastAccessTimeUtc(_exePath, DateTime.UtcNow);
                }

                processInfo.Arguments = ParseArgs();
                RunProcess(processInfo);
                return 0;
            }
            catch (Exception e)
            {
                WriteError(e);
            }

            return 1;
        }

        private static void RunProcess(ProcessStartInfo processInfo)
        {
            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
            }
        }

        private static string ParseArgs()
        {
            // Extract the arguments to be passed to the actual NuGet.exe
            // The first argument of GetCommandLineArgs is the current exe. 
            string exePath = Environment.GetCommandLineArgs()[0];

            // Find the first occurence of the exe in the CommandLine string.
            int exeIndex = Environment.CommandLine.IndexOf(exePath);

            // The first space that follows after the exe's path is the beginning of the remaining arguments.
            int argsStartIndex = Environment.CommandLine.IndexOf(' ', exeIndex + exePath.Length);

            return Environment.CommandLine.Substring(argsStartIndex + 1);
        }

        private static void WriteError(Exception e)
        {
            var currentColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.GetBaseException().Message);
            }
            finally
            {
                Console.ForegroundColor = currentColor;
            }
        }
    }
}

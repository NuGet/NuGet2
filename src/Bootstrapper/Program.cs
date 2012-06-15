using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using NuGet;

namespace Bootstrapper
{
    public class Program
    {
        public static int Main(string[] args)
        {
            string exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NuGet\NuGet.exe");
            try
            {
                var processInfo = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                if (!File.Exists(exePath))
                {
                    var document = GetConfigDocument();
                    EnsurePackageRestoreConsent(document);
                    ProxyCache.Instance = new ProxyCache(document);
                    // Register a console based credentials provider so that the user get's prompted if a password
                    // is required for the proxy
                    // Setup IHttpClient for the Gallery to locate packages
                    new HttpClient().DownloadData(exePath);
                }
                else if ((DateTime.UtcNow - File.GetLastWriteTimeUtc(exePath)).TotalDays > 10)
                {
                    // Check for updates to the exe every 10 days
                    processInfo.Arguments = "update -self";
                    RunProcess(processInfo);
                    File.SetLastWriteTimeUtc(exePath, DateTime.UtcNow);
                }
                // Convert the args list to a command line input. If an argument has any spaces in it, we need to wrap it with single quotes.
                processInfo.Arguments = String.Join(" ", args.Select(arg => arg.Any(Char.IsWhiteSpace) ? "'" + arg + "'" : arg));
                RunProcess(processInfo);
                return 0;
            }
            catch (Exception e)
            {
                WriteError(e);
            }

            return 1;
        }

        private static XmlDocument GetConfigDocument()
        {
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet", "NuGet.config");
            if (File.Exists(configPath))
            {
                var document = new XmlDocument();
                document.Load(configPath);
                return document;
            }
            return null;
        }

        private static void EnsurePackageRestoreConsent(XmlDocument document)
        {
            // Addressing this later.
            var node = document != null ? document.SelectSingleNode(@"configuration/packageRestore/add[@key='enabled']/@value") : null;
            var settingsValue = node != null ? node.Value.Trim() : "";
            var envValue = (Environment.GetEnvironmentVariable("EnableNuGetPackageRestore") ?? String.Empty).Trim();

            bool consent =  settingsValue.Equals("true", StringComparison.OrdinalIgnoreCase) || settingsValue == "1" ||
                            envValue.Equals("true", StringComparison.OrdinalIgnoreCase) || envValue == "1";
            if (!consent)
            {
                throw new InvalidOperationException(LocalizedResourceManager.GetString("RestoreConsent"));
            }
        }

        private static void RunProcess(ProcessStartInfo processInfo)
        {
            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
            }
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

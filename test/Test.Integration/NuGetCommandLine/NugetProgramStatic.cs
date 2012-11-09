using System;
using System.IO;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NugetProgramStatic : IDisposable
    {
        public NugetProgramStatic()
        {
            Program.IgnoreExtensions = true;
        }

        public void Dispose()
        {
            Program.IgnoreExtensions = false;
        }

        public static void BackupAndDeleteDefaultConfigurationFile()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string defaultConfigurationFile = Path.Combine(appDataPath, "NuGet", "NuGet.config");
            string backupFileName = defaultConfigurationFile + ".backup";

            if (File.Exists(defaultConfigurationFile))
            {
                File.Copy(defaultConfigurationFile, backupFileName, true);
                File.Delete(defaultConfigurationFile);
            }
        }

        public static void RestoreDefaultConfigurationFile()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string defaultConfigurationFile = Path.Combine(appDataPath, "NuGet", "NuGet.config");
            string backupFileName = defaultConfigurationFile + ".backup";

            if (File.Exists(backupFileName))
            {
                File.Copy(backupFileName, defaultConfigurationFile, true);
                File.Delete(backupFileName);
            }
        }  
    }
}
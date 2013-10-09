using System;
using System.IO;
using System.Threading;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    /// <summary>
    /// Helps ensuring only one unit-test can backup/restore the global nuget.config at a time.
    /// </summary>
    public class DefaultConfigurationFilePreserver: IDisposable
    {
        private const string MutexName = "DefaultConfigurationFilePreserver";
        private readonly Mutex _mutex;

        public DefaultConfigurationFilePreserver()
        {
            _mutex = new Mutex(false, MutexName);
            var owner = _mutex.WaitOne(TimeSpan.FromMinutes(2));
            if (! owner)
            {
                throw new TimeoutException(string.Format("Timedout while waiting for mutex {0}", MutexName));
            }
            BackupAndDeleteDefaultConfigurationFile();
        }

        public void Dispose()
        {
            RestoreDefaultConfigurationFile();
            _mutex.ReleaseMutex();
        }

        private static void BackupAndDeleteDefaultConfigurationFile()
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

        private static void RestoreDefaultConfigurationFile()
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

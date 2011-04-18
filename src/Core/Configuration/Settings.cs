using System;
using System.IO;

namespace NuGet {
    public static class Settings {
        private static string _nugetConfigPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet");
        private static readonly ISettings _userSettings = 
            new UserSettings(new PhysicalFileSystem(_nugetConfigPath));
        public static ISettings UserSettings {
            get {
                return _userSettings;
            }
        }
    }
}

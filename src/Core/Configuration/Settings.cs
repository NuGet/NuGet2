using System;
using System.IO;

namespace NuGet {
    public static class Settings {
        private static readonly ISettings _userSettings = 
            new UserSettings(new PhysicalFileSystem(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet")));
        public static ISettings UserSettings {
            get {
                return _userSettings;
            }
        }
    }
}

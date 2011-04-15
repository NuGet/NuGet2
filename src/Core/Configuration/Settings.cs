using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet {
    public static class Settings {
        private static readonly ISettings _userSettings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
        public static ISettings UserSettings {
            get {
                return _userSettings;
            }
        }
    }
}

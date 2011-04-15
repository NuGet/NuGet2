using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet {
    public static class Settings {
        private static ISettings _userSettings;
        public static ISettings UserSettings {
            get {
                if (_userSettings == null) {
                    _userSettings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
                }
                return _userSettings;
            }
        }
    }
}

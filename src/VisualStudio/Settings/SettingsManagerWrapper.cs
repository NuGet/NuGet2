using System;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using NuGet.VisualStudio.Types;

namespace NuGet.VisualStudio
{
    internal class SettingsManagerWrapper : ISettingsManager
    {
        private readonly ShellSettingsManager _settingsManager;

        public SettingsManagerWrapper(IServiceProvider serviceProvider)
        {
            _settingsManager = new ShellSettingsManager(serviceProvider);
        }

        public ISettingsStore GetReadOnlySettingsStore()
        {
            return new SettingsStoreWrapper(_settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings));
        }

        public IWritableSettingsStore GetWritableSettingsStore()
        {
            return new WritableSettingsStoreWrapper(_settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings));
        }
    }
}
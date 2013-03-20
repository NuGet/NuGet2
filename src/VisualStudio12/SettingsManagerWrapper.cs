using System;
using NuGet.VisualStudio.Types;

namespace NuGet.VisualStudio12
{
    public class SettingsManagerWrapper : ISettingsManager
    {
        public SettingsManagerWrapper(IServiceProvider serviceProvider)
        {
        }

        public ISettingsStore GetReadOnlySettingsStore()
        {
            return null;
        }

        public IWritableSettingsStore GetWritableSettingsStore()
        {
            return null;
        }
    }
}
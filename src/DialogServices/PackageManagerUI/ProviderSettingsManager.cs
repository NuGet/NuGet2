using System;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    internal class ProviderSettingsManager : SettingsManagerBase, IProviderSettings
    {
        private const string SettingsRoot = "NuGet";
        private const string SelectedPropertyName = "SelectedProvider";
        private const string IncludePrereleaseName = "Prerelease";

        public ProviderSettingsManager() :
            base(ServiceLocator.GetInstance<IServiceProvider>())
        {
        }

        public int SelectedProvider
        {
            get
            {
                return Math.Max(0, ReadInt32(SettingsRoot, SelectedPropertyName));
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                WriteInt32(SettingsRoot, SelectedPropertyName, value);
            }
        }


        public bool IncludePrereleasePackages
        {
            get
            {
                return ReadBool(SettingsRoot, IncludePrereleaseName);
            }
            set
            {
                WriteBool(SettingsRoot, IncludePrereleaseName, value);
            }
        }
    }
}
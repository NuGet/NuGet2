using System;
using System.ComponentModel.Composition;
using NuGet.VisualStudio;

namespace NuGet.Dialog {
    [Export(typeof(ISelectedProviderSettings))]
    internal class SelectedProviderSettingsManager : SettingsManagerBase, ISelectedProviderSettings {
        private const string SettingsRoot = "NuGet";
        private const string PropertyName = "SelectedProvider";

        public SelectedProviderSettingsManager() : 
            base(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public int SelectedProvider {
            get {
                return Math.Max(0, ReadInt32(SettingsRoot, PropertyName));
            }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value");
                }
                WriteInt32(SettingsRoot, PropertyName, value);
            }
        }
    }
}
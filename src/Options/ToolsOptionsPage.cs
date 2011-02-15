using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace NuGet.Options {
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Interoperability", 
        "CA1408:DoNotUseAutoDualClassInterfaceType")]
    [Guid("2819C3B6-FC75-4CD5-8C77-877903DE864C")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ToolsOptionsPage : DialogPage, IServiceProvider {
        private ToolsOptionsControl _optionsWindow;

        //We override the base implementation of LoadSettingsFromStorage and SaveSettingsToStorage
        //since we already provide settings persistance using the SettingsManager. These two APIs
        //will read/write the tools/options properties to an alternate location, which can cause
        //incorrect behavior if the two copies of the data are out of sync.
        public override void LoadSettingsFromStorage() { }

        public override void SaveSettingsToStorage() { }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override System.Windows.Forms.IWin32Window Window {
            get {
                return this.OptionsControl;
            }
        }

        protected override void OnActivate(CancelEventArgs e) {
            base.OnActivate(e);
            this.OptionsControl.Font = VsShellUtilities.GetEnvironmentFont(this);
            this.OptionsControl.InitializeOnActivated();
        }

        protected override void OnApply(PageApplyEventArgs e) {
            // Do not need to call base.OnApply() here.
            bool wasApplied = this.OptionsControl.ApplyChangedSettings();
            if (!wasApplied) {
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
            }
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            this.OptionsControl.ClearSettings();
        }

        private ToolsOptionsControl OptionsControl {
            get {
                if (_optionsWindow == null) {
                    _optionsWindow = new ToolsOptionsControl(this);
                    _optionsWindow.Location = new Point(0, 0);
                }

                return _optionsWindow;
            }
        }

        object IServiceProvider.GetService(Type serviceType) {
            return this.GetService(serviceType);
        }
    }
}

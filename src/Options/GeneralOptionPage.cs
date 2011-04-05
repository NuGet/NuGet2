using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace NuGet.Options {
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Interoperability",
        "CA1408:DoNotUseAutoDualClassInterfaceType")]
    [Guid("0F052CF7-BF62-4743-B190-87FA4D49421E")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class GeneralOptionPage : DialogPage, IServiceProvider {
        private GeneralOptionControl _optionsWindow;

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
            OptionsControl.Font = VsShellUtilities.GetEnvironmentFont(this);
            OptionsControl.OnActivated();
        }

        protected override void OnApply(PageApplyEventArgs e) {
            base.OnApply(e);
            OptionsControl.OnApply();
        }

        private GeneralOptionControl OptionsControl {
            get {
                if (_optionsWindow == null) {
                    _optionsWindow = new GeneralOptionControl();
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
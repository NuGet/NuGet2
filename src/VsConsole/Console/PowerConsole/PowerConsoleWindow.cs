using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace NuGetConsole.Implementation.PowerConsole {
    [Export(typeof(IPowerConsoleWindow))]
    class PowerConsoleWindow : IPowerConsoleWindow {
        public const string ContentType = "PackageConsole";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [
            Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import]
        internal IWpfConsoleService WpfConsoleService { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [ImportMany]
        internal IEnumerable<Lazy<IHostProvider, IHostMetadata>> HostProviders { get; set; }

        internal event EventHandler ActiveHostChanged;

        Dictionary<string, HostInfo> _hostInfos;
        Dictionary<string, HostInfo> HostInfos {
            get {
                if (_hostInfos == null) {
                    _hostInfos = new Dictionary<string, HostInfo>();
                    foreach (Lazy<IHostProvider, IHostMetadata> p in HostProviders) {
                        HostInfo info = new HostInfo(this, p);
                        _hostInfos[info.HostName] = info;
                    }
                }
                return _hostInfos;
            }
        }

        HostInfo _activeHostInfo;
        internal HostInfo ActiveHostInfo {
            get {
                if (_activeHostInfo == null) {
                    if (!string.IsNullOrEmpty(ActiveHost)) {
                        HostInfos.TryGetValue(ActiveHost, out _activeHostInfo);
                    }
                }
                return _activeHostInfo;
            }
        }

        #region IPowerConsole
        public IEnumerable<string> Hosts {
            get { return HostProviders.Select(p => p.Metadata.HostName); }
        }

        string _activeHost;

        public string ActiveHost {
            get {
                if (_activeHost == null) {
                    Settings.GetDefaultHost(ServiceProvider, out _activeHost);
                }

                // If _activeHost is invalid (e.g., a previous provider is uninstalled),
                // simply use a random available host
                if (string.IsNullOrEmpty(_activeHost) || !HostInfos.ContainsKey(_activeHost)) {
                    _activeHost = HostInfos.Keys.FirstOrDefault();
                }

                return _activeHost;
            }
            set {
                if (!string.Equals(_activeHost, value) && HostInfos.ContainsKey(value)) {
                    _activeHost = value;
                    _activeHostInfo = null;
                    this.ActiveHostChanged.Raise(this);
                }
            }
        }

        // represent the default feed
        public string ActiveHostSetting {
            get {
                HostInfo hi = ActiveHostInfo;
                return (hi != null && hi.WpfConsole != null && hi.WpfConsole.Host != null) ?
                    ActiveHostInfo.WpfConsole.Host.Setting :
                    null;
            }
            set {
                HostInfo hi = ActiveHostInfo;
                if (hi != null && hi.WpfConsole != null && hi.WpfConsole.Host != null) {
                    hi.WpfConsole.Host.Setting = value;
                }
            }
        }

        public string[] AvailableHostSettings {
            get {
                return ActiveHostInfo.WpfConsole.Host.GetAvailableSettings();
            }
        }

        public string[] AvailableProjects {
            get {
                return ActiveHostInfo.WpfConsole.Host.GetAvailableProjects();
            }
        }

        public string DefaultProject {
            get {
                HostInfo hi = ActiveHostInfo;
                return (hi != null && hi.WpfConsole != null && hi.WpfConsole.Host != null) ?
                    ActiveHostInfo.WpfConsole.Host.DefaultProject :
                    null;
            }
            set {
                HostInfo hi = ActiveHostInfo;
                if (hi != null && hi.WpfConsole != null && hi.WpfConsole.Host != null) {
                    hi.WpfConsole.Host.DefaultProject = value;
                }
            }
        }

        public void Show() {
            IVsUIShell vsUIShell = ServiceProvider.GetService<IVsUIShell>(typeof(SVsUIShell));
            if (vsUIShell != null) {
                Guid guid = typeof(PowerConsoleToolWindow).GUID;
                IVsWindowFrame frame;

                ErrorHandler.ThrowOnFailure(
                    vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guid, out frame));

                if (frame != null) {
                    ErrorHandler.ThrowOnFailure(frame.Show());
                }
            }
        }
        #endregion
    }
}

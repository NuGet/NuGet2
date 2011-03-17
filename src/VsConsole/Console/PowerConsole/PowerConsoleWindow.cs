using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGetConsole.Implementation.PowerConsole {
    [Export(typeof(IPowerConsoleWindow))]
    [Export(typeof(IHostInitializer))]
    internal class PowerConsoleWindow : IPowerConsoleWindow, IHostInitializer {
        public const string ContentType = "PackageConsole";

        private string _activeHost;
        private Dictionary<string, HostInfo> _hostInfos;
        private HostInfo _activeHostInfo;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import]
        internal IWpfConsoleService WpfConsoleService { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [ImportMany]
        internal IEnumerable<Lazy<IHostProvider, IHostMetadata>> HostProviders { get; set; }

        internal event EventHandler ActiveHostChanged;

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

        public IEnumerable<string> Hosts {
            get { return HostProviders.Select(p => p.Metadata.HostName); }
        }

        public string ActiveHost {
            get {
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
                    ActiveHostChanged.Raise(this);
                }
            }
        }

        // represent the default feed
        public string ActivePackageSource {
            get {
                HostInfo hi = ActiveHostInfo;
                return (hi != null && hi.WpfConsole != null && hi.WpfConsole.Host != null) ?
                    ActiveHostInfo.WpfConsole.Host.ActivePackageSource :
                    null;
            }
            set {
                HostInfo hi = ActiveHostInfo;
                if (hi != null && hi.WpfConsole != null && hi.WpfConsole.Host != null) {
                    hi.WpfConsole.Host.ActivePackageSource = value;
                }
            }
        }

        public string[] PackageSources {
            get {
                return ActiveHostInfo.WpfConsole.Host.GetPackageSources();
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

        public void Start() {
            ActiveHostInfo.WpfConsole.Dispatcher.Start();
        }

        public void SetDefaultRunspace() {
            ActiveHostInfo.WpfConsole.Host.SetDefaultRunspace();
        }
    }
}

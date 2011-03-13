using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal class NuGetPSHost : PSHost {
        private readonly string _name;
        private readonly PSObject _privateData;

        public IConsole ActiveConsole { get; set; }

        public NuGetPSHost(string name, params Tuple<string, object>[] extraData) {
            _name = name;
            _privateData = new PSObject(new Commander(this));

            // add extra data as note properties
            foreach (var tuple in extraData) {
                _privateData.Properties.Add(new PSNoteProperty(tuple.Item1, tuple.Item2));
            }
        }

        CultureInfo _culture = Thread.CurrentThread.CurrentCulture;
        public override CultureInfo CurrentCulture {
            get { return _culture; }
        }

        CultureInfo _uiCulture = Thread.CurrentThread.CurrentUICulture;
        public override CultureInfo CurrentUICulture {
            get { return _uiCulture; }
        }

        public override void EnterNestedPrompt() {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt() {
            throw new NotImplementedException();
        }

        Guid _instanceId = Guid.NewGuid();
        public override Guid InstanceId {
            get { return _instanceId; }
        }

        public override string Name {
            get { return _name; }
        }

        public override void NotifyBeginApplication() {
        }

        public override void NotifyEndApplication() {
        }

        public override PSObject PrivateData {
            get {
                return _privateData;
            }
        }

        public override void SetShouldExit(int exitCode) {
            //TODO: Exit VS?
        }

        private PSHostUserInterface _ui;
        public override PSHostUserInterface UI {
            get {
                if (_ui == null) {
                    _ui = new NuGetHostUserInterface(this);
                }
                return _ui;
            }
        }

        public override Version Version {
            get {
                return this.GetType().Assembly.GetName().Version;
            }
        }

        private class Commander {
            private NuGetPSHost _host;

            public Commander(NuGetPSHost host) {
                _host = host;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "This method can be dynamically invoked from PS script.")]
            public void ClearHost() {
                if (_host.ActiveConsole != null) {
                    _host.ActiveConsole.Clear();
                }
            }
        }
    }
}
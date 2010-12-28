using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;

namespace NuGetConsole.Host.PowerShell.Implementation {
    class MyHost : PSHost {
        PowerShellHost _host;
        string _name;
        PSObject _privateData;

        public MyHost(PowerShellHost host, string name, object privateData) {
            UtilityMethods.ThrowIfArgumentNull(host);

            _host = host;
            _name = name;
            _privateData = (privateData != null ? new PSObject(privateData) : null);
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

        PSHostUserInterface _ui;
        public override PSHostUserInterface UI {
            get {
                if (_ui == null) {
                    _ui = new MyHostUI(_host.Console);
                }
                return _ui;
            }
        }

        public override Version Version {
            get {
                return this.GetType().Assembly.GetName().Version;
            }
        }
    }
}

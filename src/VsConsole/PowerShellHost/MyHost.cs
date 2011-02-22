using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal class MyHost : PSHost {
        private PowerShellHost _host;
        private string _name;
        private PSObject _privateData;

        public MyHost(PowerShellHost host, string name, object privateData, params Tuple<string, object>[] extraData) {
            UtilityMethods.ThrowIfArgumentNull(host);

            _host = host;
            _name = name;
            _privateData = (privateData != null ? new PSObject(privateData) : new PSObject());

            // add extra data as note properties
            foreach (var tuple in extraData) {
                _privateData.Properties.Add(new PSNoteProperty(tuple.Item1, tuple.Item2));
            }

            // add the flag to indicate whether our host is operating in sync mode
            _privateData.Properties.Add(new PSNoteProperty("IsSyncMode", !host.IsAsync));
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

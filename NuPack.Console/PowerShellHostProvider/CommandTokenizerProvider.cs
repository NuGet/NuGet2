using System;
using System.ComponentModel.Composition;
using NuPackConsole.Host.PowerShell.Implementation;

namespace NuPackConsole.Host.PowerShellProvider {

    [Export(typeof(ICommandTokenizerProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    class CommandTokenizerProvider : ICommandTokenizerProvider {
        private Lazy<CommandTokenizer> _instance = new Lazy<CommandTokenizer>(() => new CommandTokenizer());

        public ICommandTokenizer Create(IHost host) {
            return _instance.Value;
        }
    }
}

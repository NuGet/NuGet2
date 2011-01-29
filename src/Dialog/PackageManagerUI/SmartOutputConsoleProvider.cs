using NuGet.OutputWindowConsole;
using NuGetConsole;

namespace NuGet.Dialog.PackageManagerUI {
    internal class SmartOutputConsoleProvider : IOutputConsoleProvider {

        private readonly IOutputConsoleProvider _baseProvider;
        private bool _isFirstTime = true;

        public SmartOutputConsoleProvider(IOutputConsoleProvider baseProvider) {
            _baseProvider = baseProvider;
        }

        public IConsole CreateOutputConsole(bool requirePowerShellHost) {
            IConsole console = _baseProvider.CreateOutputConsole(requirePowerShellHost);

            if (_isFirstTime) {
                // the first time the console is accessed after dialog is opened, we clear the console.
                console.Clear();
                _isFirstTime = false;
            }

            return console;
        }
    }
}
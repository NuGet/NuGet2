using Moq;
using NuGet.OutputWindowConsole;
using NuGetConsole;

namespace NuGet.Dialog.Test {
    public class MockOutputConsoleProvider : IOutputConsoleProvider {
        public IConsole CreateOutputConsole(bool requirePowerShellHost) {
            return new Mock<IConsole>().Object;
        }
    }
}

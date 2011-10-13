using Moq;
using NuGet.VisualStudio;
using NuGetConsole;

namespace NuGet.Dialog.Test
{
    public class MockOutputConsoleProvider : IOutputConsoleProvider
    {
        public IConsole CreateOutputConsole(bool requirePowerShellHost)
        {
            return new Mock<IConsole>().Object;
        }
    }
}

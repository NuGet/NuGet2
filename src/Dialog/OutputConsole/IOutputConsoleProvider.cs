using NuGetConsole;

namespace NuGet.OutputWindowConsole {
    public interface IOutputConsoleProvider {
        IConsole CreateOutputConsole(bool requirePowerShellHost);
    }
}

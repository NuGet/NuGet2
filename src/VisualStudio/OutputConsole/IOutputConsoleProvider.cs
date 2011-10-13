using NuGetConsole;

namespace NuGet.VisualStudio
{
    public interface IOutputConsoleProvider
    {
        IConsole CreateOutputConsole(bool requirePowerShellHost);
    }
}

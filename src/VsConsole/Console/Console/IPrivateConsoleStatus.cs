
namespace NuGetConsole.Implementation.Console {
    internal interface IPrivateConsoleStatus : IConsoleStatus {
        void SetBusy(bool isBusy);
    }
}


namespace NuGet.Dialog.PackageManagerUI {
    public interface IProgressWindowOpener {
        void Show(string title);
        void Hide();
        bool IsOpen { get; }
        bool Close();
        void SetCompleted(bool successful);
        void AddMessage(LogMessageLevel level, string message);
        void ShowProgress(string operation, int percentComplete);
    }

    public enum LogMessageLevel {
        Info = (int)MessageLevel.Info,
        Warning = (int)MessageLevel.Warning,
        Debug = (int)MessageLevel.Debug,
        Error
    }
}
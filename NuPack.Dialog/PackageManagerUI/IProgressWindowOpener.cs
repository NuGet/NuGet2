
namespace NuGet.Dialog.PackageManagerUI {
    public interface IProgressWindowOpener {
        void Show(string title);
        void Hide();
        bool IsOpen { get; }
        bool Close();
        void SetCompleted();
    }
}
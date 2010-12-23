
namespace NuGet.Dialog.PackageManagerUI {
    public interface IProgressWindowOpener {
        bool? ShowModal(string title);
        bool IsOpen { get; }
        bool Close();
        void SetCompleted();
    }
}
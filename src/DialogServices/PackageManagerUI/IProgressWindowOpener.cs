using System.Windows;

namespace NuGet.Dialog.PackageManagerUI
{
    public interface IProgressWindowOpener
    {
        bool IsOpen { get; }
        void Show(string title, bool cancelable);
        bool UpdateMessageAndQueryStatus(string message);
        bool Close();
    }
}
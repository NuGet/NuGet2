
namespace NuGet.Dialog.PackageManagerUI
{
    public interface IUpdateAllUIService 
    {
        void Show();
        void Hide();
        UpdateAllUI CreateUIElement();
        void DisposeElement();
    }
}
using System.Windows;

namespace NuGet.VisualStudio
{
    public interface IWindowSettingsManager
    {
        Size GetWindowSize(string windowToken);
        void SetWindowSize(string windowToken, Size size);
    }
}
using System;

namespace NuGet.VisualStudio
{
    public interface IProductUpdateService
    {
        void CheckForAvailableUpdateAsync();
        void Update();
        void DeclineUpdate(bool doNotRemindAgain);
        event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable;
    }
}
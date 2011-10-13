using System;
using NuGet.VisualStudio;

namespace NuGet.Cmdlets.Test
{
    internal class MockProductUpdateService : IProductUpdateService
    {
        public void CheckForAvailableUpdateAsync()
        {
            if (UpdateAvailable != null)
            {
                UpdateAvailable(this, new ProductUpdateAvailableEventArgs(new Version("1.0"), new Version("2.0")));
            }
        }

        public void Update()
        {
        }

        public void DeclineUpdate(bool doNotRemindAgain)
        {
        }

        public event EventHandler<ProductUpdateAvailableEventArgs> UpdateAvailable;
    }
}
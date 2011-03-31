using System.Threading.Tasks;
using System;

namespace NuGet.VisualStudio {
    public interface IProductUpdateService {
        void CheckForAvailableUpdateAsync();
        void Update();
        void DeclineUpdate();
        event EventHandler UpdateAvailable;
    }
}
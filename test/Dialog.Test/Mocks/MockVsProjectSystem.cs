using NuGet.Test.Mocks;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test
{
    internal class MockVsProjectSystem : MockProjectSystem, IVsProjectSystem
    {
        public string UniqueName
        {
            get { return "Unique Name"; }
        }
    }
}

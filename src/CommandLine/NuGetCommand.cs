using System.Resources;

namespace NuGet
{
    internal class NuGetCommand
    {
        private static readonly ResourceManager resourceMan = new ResourceManager("NuGet.NuGetCommand", typeof(NuGetCommand).Assembly);

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        internal static ResourceManager ResourceManager
        {
            get
            {
                return resourceMan;
            }
        }
    }
}

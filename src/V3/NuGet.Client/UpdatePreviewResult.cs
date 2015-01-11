
using NuGet.PackagingCore;
namespace NuGet.Client
{
    public class UpdatePreviewResult
    {
        public PackageIdentity Old { get; private set; }
        public PackageIdentity New { get; private set; }

        public UpdatePreviewResult(PackageIdentity oldPackage, PackageIdentity newPackage)
        {
            Old = oldPackage;
            New = newPackage;
        }

        public override string ToString()
        {
            return Old.ToString() + " -> " + New.ToString();
        }
    }
}

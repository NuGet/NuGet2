
namespace NuGet.VisualStudio
{
    public interface IVsPackageSourceProvider : IPackageSourceProvider
    {
        PackageSource ActivePackageSource { get; set; }
    }
}
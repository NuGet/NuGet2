
namespace NuGet.VisualStudio {
    public interface IOptionsDialogOpener {
        void OpenOptionsDialog(NuGetOptionsPage activePage);
    }

    public enum NuGetOptionsPage {
        PackageSources,
        RecentPackages
    }
}
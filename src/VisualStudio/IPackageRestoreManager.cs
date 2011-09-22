
namespace NuGet.VisualStudio {
    public interface IPackageRestoreManager {
        bool IsCurrentSolutionEnabled { get; }
        void EnableCurrentSolution(bool quietMode);
    }
}
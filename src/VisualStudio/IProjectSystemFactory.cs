using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface IProjectSystemFactory 
    {
        IProjectSystem CreateProjectSystem(Project project, IFileSystemProvider fileSystemProvider);
    }
}

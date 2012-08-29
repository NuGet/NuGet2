
namespace Microsoft.VisualStudio.Project
{
    public interface IDirectAccess
    {
        ProjectAccess ProjectAccess { get; }
        Microsoft.Build.Evaluation.Project GetProject(ConfiguredProject configuredProject);
    }
}

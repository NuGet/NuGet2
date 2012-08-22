namespace Microsoft.VisualStudio.Project
{
    public interface UnconfiguredProject
    {
        ProjectService ProjectService { get; }
        string FullPath { get; set; }
        IUnconfiguredProjectServices Services { get; set; }
    }
}

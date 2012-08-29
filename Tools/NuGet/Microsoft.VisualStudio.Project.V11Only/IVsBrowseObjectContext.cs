
namespace Microsoft.VisualStudio.Project.Designers
{
    public interface IVsBrowseObjectContext
    {
        ConfiguredProject ConfiguredProject { get; }
        UnconfiguredProject UnconfiguredProject { get; }
    }
}

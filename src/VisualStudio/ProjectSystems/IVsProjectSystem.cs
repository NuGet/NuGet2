namespace NuGet.VisualStudio
{
    public interface IVsProjectSystem : IProjectSystem
    {
        string UniqueName { get; }
    }
}
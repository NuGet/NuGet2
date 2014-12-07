namespace NuGet.Client.Resolution
{
    public class ResolutionContext
    {
        public DependencyBehavior DependencyBehavior { get; set; }

        public bool AllowPrerelease { get; set; }

        public bool ForceRemove { get; set; }

        public bool RemoveDependencies { get; set; }
    }
}
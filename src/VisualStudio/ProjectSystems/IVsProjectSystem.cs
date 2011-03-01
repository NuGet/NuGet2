namespace NuGet.VisualStudio {
    internal interface IVsProjectSystem : IProjectSystem {
        string UniqueName { get; }
    }
}

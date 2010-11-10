using System.Runtime.Versioning;

namespace NuGet {
    public interface IProjectSystem : IFileSystem {
        FrameworkName TargetFramework { get; }
        dynamic GetPropertyValue(string propertyName);
        string ProjectName { get; }

        void AddReference(string referencePath);
        bool ReferenceExists(string name);
        void RemoveReference(string name);
        bool IsSupportedFile(string path);
    }
}

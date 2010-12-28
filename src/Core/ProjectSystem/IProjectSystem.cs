using System.IO;
using System.Runtime.Versioning;

namespace NuGet {
    public interface IProjectSystem : IFileSystem {
        FrameworkName TargetFramework { get; }
        dynamic GetPropertyValue(string propertyName);
        string ProjectName { get; }

        /// <summary>
        /// Method called when adding a assembly reference to the project.
        /// </summary>
        /// <param name="referencePath">Physical path to the assembly file relative to the project root.</param>
        /// <param name="stream">Contents of the assembly file.</param>
        void AddReference(string referencePath, Stream stream);
        bool ReferenceExists(string name);
        void RemoveReference(string name);
        bool IsSupportedFile(string path);
    }
}

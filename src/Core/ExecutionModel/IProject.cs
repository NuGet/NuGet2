using System.Collections.Generic;
using System.IO;

namespace NuGet.ExecutionModel
{
    public interface IProjectProxy
    {
        string TargetFramework { get; }
        string ProjectName { get; }
        string Root { get; }

        void DeleteDirectory(string path, bool recursive);
        IEnumerable<string> GetFiles(string path, string filter, bool recursive);
        IEnumerable<string> GetDirectories(string path);
        void DeleteFile(string path);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        void AddFile(string path, Stream stream);

        /// <summary>
        /// Adds an assembly reference to a framework assembly (one in the GAC).
        /// </summary>
        /// <param name="assemblyName">name of the assembly</param>
        void AddFrameworkReference(string assemblyName);

        /// <summary>
        /// Add an assembly reference to the project.
        /// </summary>
        /// <param name="assemblyPath">Physical path to the assembly file relative to the project root.</param>
        void AddReference(string assemblyPath);
        bool ReferenceExists(string name);
        void RemoveReference(string name);
        bool IsBindingRedirectSupported { get; }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Build.Evaluation;

namespace NuGet.Common {
    public class MSBuildProjectSystem : PhysicalFileSystem, IProjectSystem, IMSBuildProjectSystem {
        public MSBuildProjectSystem(string projectFile)
            : base(Path.GetDirectoryName(projectFile)) {
            Project = GetProject(projectFile);
        }

        private Project Project {
            get;
            set;
        }

        public void AddFrameworkReference(string name) {
            // No-op
        }

        public void AddReference(string referencePath, Stream stream) {
            string fullPath = PathUtility.GetAbsolutePath(Root, referencePath);
            string relativePath = PathUtility.GetRelativePath(Project.FullPath, fullPath);
            // REVIEW: Do we need to use the fully qualified the assembly name for strong named assemblies?
            string include = Path.GetFileNameWithoutExtension(fullPath);

            Project.AddItem("Reference",
                            include,
                            new[] { 
                                    new KeyValuePair<string, string>("HintPath", relativePath)
                                });
        }

        public dynamic GetPropertyValue(string propertyName) {
            return Project.GetPropertyValue(propertyName);
        }

        public bool IsSupportedFile(string path) {
            return true;
        }

        public string ProjectName {
            get {
                return Path.GetFileNameWithoutExtension(Project.FullPath);
            }
        }

        public bool ReferenceExists(string name) {
            return GetReference(name) != null;
        }

        public void RemoveReference(string name) {
            ProjectItem assemblyReference = GetReference(name);
            if (assemblyReference != null) {
                Project.RemoveItem(assemblyReference);
            }
        }

        private ProjectItem GetItem(string itemType, string name) {
            return Project.GetItems(itemType).FirstOrDefault(i => i.EvaluatedInclude.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<ProjectItem> GetItems(string itemType, string name) {
            return Project.GetItems(itemType).Where(i => i.EvaluatedInclude.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }

        public ProjectItem GetReference(string name) {
            name = Path.GetFileNameWithoutExtension(name);
            return GetItems("Reference", name)
                   .Where(item => new AssemblyName(item.EvaluatedInclude).Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                   .FirstOrDefault();
        }

        public FrameworkName TargetFramework {
            get {
                string moniker = GetPropertyValue("TargetFrameworkMoniker");
                if (String.IsNullOrEmpty(moniker)) {
                    return null;
                }
                return new FrameworkName(moniker);
            }
        }

        public string ResolvePath(string path) {
            return path;
        }

        public void Save() {
            Project.Save();
        }

        private Project GetProject(string projectFile) {
            return ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFile).FirstOrDefault() ?? new Project(projectFile);
        }
    }
}

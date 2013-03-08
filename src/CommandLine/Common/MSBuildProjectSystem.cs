using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Microsoft.Build.BuildEngine;

// To support xbuild, we have to use Microsoft.Build.BuildEngine that
// is marked deprecated. Disable the warning.
#pragma warning disable 618

namespace NuGet.Common
{
    public enum Language
    {
        CSharp,
        VB,
        FSharp,
        JavaScript,
        Cpp,
        None // e.g. wixproject 
    }
	
    public class MSBuildProjectSystem : PhysicalFileSystem, IMSBuildProjectSystem, IEquatable<MSBuildProjectSystem>
    {
        private const string BinDir = "bin";

        private static ReadOnlyHashSet<string> _knownProjectExtensions = 
            new ReadOnlyHashSet<string> (
                new [] {".csproj", ".vbproj", ".fsproj", ".jsproj", ".wixproj" },
                StringComparer.OrdinalIgnoreCase);

        private Language _language;

        public static MSBuildProjectSystem Create(string projectFile)
        {
            var project = GetProject(projectFile);

            Language lang = Language.None;
            switch (Path.GetExtension(projectFile).ToUpperInvariant())
            {
                case ".CSPROJ":
                    lang = Language.CSharp;
                    break;
                case ".VBPROJ":
                    lang = Language.VB;
                    break;
                case ".FSPROJ":
                    lang = Language.FSharp;
                    break;
                case ".JSPROJ":
                    lang = Language.JavaScript;
                    break;
                case ".WIXPROJ":
                    lang = Language.None;
                    break;
                default:
                    throw new InvalidOperationException("Unknown project type");
            }

            var projectTypeGuidsValue = project.GetEvaluatedProperty("ProjectTypeGuids");
            string[] projectTypeGuids = new string[] { };
            if (projectTypeGuidsValue != null)
            {
                projectTypeGuids = projectTypeGuidsValue.Split(';');
            }

            foreach (var projectType in projectTypeGuids)
            {
                if (projectType.Equals(CommandLineConstants.WebApplicationProjectTypeGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return new WebApplicatioProject(project, lang);
                }
            }

            return new MSBuildProjectSystem(project, lang);
        }

        protected MSBuildProjectSystem(Project project, Language language)
            : base(Path.GetDirectoryName(project.FullFileName))
        {
            Project = project;
            _language = language;
        }

        public static ReadOnlyHashSet<string> KnownProjectExtensions
        {
            get { return _knownProjectExtensions; }
        }
 
        public bool IsBindingRedirectSupported
        {
            get
            {
                return true;
            }
        }

        private Project Project
        {
            get;
            set;
        }

        public void AddFrameworkReference(string name)
        {
            Project.AddNewItem("Reference", name);
        }

        public void AddReference(string referencePath, Stream stream)
        {
            string fullPath = PathUtility.GetAbsolutePath(Root, referencePath);
            string relativePath = PathUtility.GetRelativePath(Project.FullFileName, fullPath)
                .Replace('/', '\\');

            string include = Path.GetFileNameWithoutExtension(fullPath);
            var item = Project.AddNewItem("Reference", include);
            item.SetMetadata("HintPath", relativePath);
        }

        public override void AddFile(string path, Stream stream)
        {
            AddFileCore(path, () => base.AddFile(path, stream));
        }

        public override void AddFile(string path, Action<Stream> writeToStream)
        {
            AddFileCore(path, () => base.AddFile(path, writeToStream));
        }

        public override void DeleteFile(string path)
        {
            base.DeleteFile(path);

            path = path.Replace('/', '\\');
            BuildItem item = Project.ItemGroups.OfType<BuildItemGroup>()
                .SelectMany(itemGroup => itemGroup.OfType<BuildItem>())
                .FirstOrDefault(i => i.Include.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                Project.RemoveItem(item);
            }            
        }

        private void AddFileCore(string path, Action addFile)
        {
            bool fileExistsInProject = FileExistsInProject(path);

            // If the file exists on disk but not in the project then skip it
            if (base.FileExists(path) && !fileExistsInProject)
            {
                Logger.Log(MessageLevel.Warning, NuGetResources.Warning_FileExists, path);
            }
            else
            {
                // TODO: EnsureCheckedOutIfExists(path);
                addFile();
                if (!fileExistsInProject)
                {
                    AddFileToProject(path);
                }
            }
        }

        protected virtual bool ExcludeFile(string path)
        {
            // Exclude files from the bin directory.
            return Path.GetDirectoryName(path).Equals(BinDir, StringComparison.OrdinalIgnoreCase);
        }

        public virtual void AddFileToProject(string path)
        {
            if (ExcludeFile(path))
            {
                return;
            }

            string fullPath = Path.Combine(Root, path);
            string relativePath = PathUtility.GetRelativePath(Project.FullFileName, fullPath)
                .Replace('/', '\\');
            
            if (!FileExistsInProject(relativePath))
            {
                Project.AddNewItem(GetBuildAction(relativePath), relativePath);
            }
        }

        protected virtual string GetBuildAction(string relativePath)
        {
            switch (Path.GetExtension(relativePath).ToUpperInvariant())
            {
                case ".CONFIG":
                    return "None";
                case ".RESX":
                    return "EmbeddedResource";
                case ".CS" :
                    return _language == Language.CSharp ? 
                        "Compile" :
                        "Content";
                case ".VB" :
                    return _language == Language.VB ? 
                        "Compile" :
                        "Content";
                case ".FS":
                    return _language == Language.FSharp ?
                        "Compile" :
                        "Content";
                case ".JS":
                    return _language == Language.JavaScript ?
                        "Compile" :
                        "Content";
                case ".CPP":
                    return _language == Language.Cpp ?
                        "Compile" :
                        "Content";
                default:
                    return "Content";
            }
        }

        public dynamic GetPropertyValue(string propertyName)
        {
            return Project.GetEvaluatedProperty(propertyName);
        }

        public virtual bool IsSupportedFile(string path)
        {
            return true;
        }

        public string ProjectName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Project.FullFileName);
            }
        }

        public bool ReferenceExists(string name)
        {
            return GetReference(name) != null;
        }

        public void RemoveReference(string name)
        {
            BuildItem assemblyReference = GetReference(name);
            if (assemblyReference != null)
            {
                Project.RemoveItem(assemblyReference);
            }
        }

        private IEnumerable<BuildItem> GetItems(string itemType, string name)
        {
            return Project.GetEvaluatedItemsByName(itemType).OfType<BuildItem>()
                .Where(i => i.Include.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }

        public BuildItem GetReference(string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            return GetItems("Reference", name)
                .FirstOrDefault(
                    item =>
                    new AssemblyName(item.Include).Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public FrameworkName TargetFramework
        {
            get
            {
                string moniker = GetPropertyValue("TargetFrameworkMoniker");
                if (String.IsNullOrEmpty(moniker))
                {
                    return null;
                }
                return new FrameworkName(moniker);
            }
        }

        public string ResolvePath(string path)
        {
            return path;
        }

        public void Save()
        {
            Project.Save(Project.FullFileName);
        }

        public bool FileExistsInProject(string path)
        {
            // some ItemTypes which starts with _ are added by various MSBuild tasks for their own purposes
            // and they do not represent content files of the projects. Therefore, we exclude them when checking for file existence.
            return Project.ItemGroups.OfType<BuildItemGroup>().SelectMany(itemGroup => itemGroup.OfType<BuildItem>())
                .Any(i => i.Include.Equals(path, StringComparison.OrdinalIgnoreCase) &&
                     (String.IsNullOrEmpty(i.Name) || i.Name[0] != '_'));
        }

        private static Project GetProject(string projectFile)
        {
            Project project = new Project();
            project.Load(projectFile);
            return project;
        }

        public void AddImport(string targetFullPath, ProjectImportLocation location)
        {
            if (targetFullPath == null)
            {
                throw new ArgumentNullException("targetFullPath");
            }

            var targetRelativePath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), targetFullPath);

            Save();
            var proj = new Microsoft.Build.Evaluation.Project(Project.FullFileName);
            NuGet.MSBuildProjectUtility.AddImportStatement(proj, targetRelativePath, location);
            proj.Save();
            Project = GetProject(Project.FullFileName);
        }

        public void RemoveImport(string targetFullPath)
        {
            if (targetFullPath == null)
            {
                throw new ArgumentNullException("targetFullPath");
            }
            
            var targetRelativePath = PathUtility.GetRelativePath(PathUtility.EnsureTrailingSlash(Root), targetFullPath);

            Save();
            var proj = new Microsoft.Build.Evaluation.Project(Project.FullFileName);            
            NuGet.MSBuildProjectUtility.RemoveImportStatement(proj, targetRelativePath);
            proj.Save();
            Project = GetProject(Project.FullFileName);
        }

        public bool Equals(MSBuildProjectSystem other)
        {
            if (other == null)
            {
                return false;
            }

            return Project.FullFileName.Equals(other.Project.FullFileName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
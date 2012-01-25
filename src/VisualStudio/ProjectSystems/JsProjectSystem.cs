using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// This project system represents the JavaScript Metro project in Windows8
    /// </summary>
    public class JsProjectSystem : VsProjectSystem
    {
        public JsProjectSystem(Project project, IFileSystemProvider fileSystemProvider) :
            base(project, fileSystemProvider)
        {
        }

        public override string ProjectName
        {
            get
            {
                return Project.GetName();
            }
        }

        public override void AddFile(string path, Stream stream)
        {
            Project.GetProjectItems(path, createIfNotExists: true);
            base.AddFile(path, stream);
        }

        protected override void AddFileToContainer(string fullPath, ProjectItems container)
        {
            container.AddFromFile(fullPath);
        }
    }
}
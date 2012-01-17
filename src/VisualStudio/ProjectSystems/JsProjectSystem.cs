using EnvDTE;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// This project system represents the JavaScript Metro project in Windows8
    /// </summary>
    public class JsProjectSystem : VsProjectSystem
    {
        public JsProjectSystem(Project project) :
            base(project)
        {
        }

        public override string ProjectName
        {
            get
            {
                return Project.GetName();
            }
        }

        protected override void EnsureDirectory(string path)
        {
            Project.GetProjectItems(path, createIfNotExists: true);
        }

        protected override void AddFileToContainer(string fullPath, ProjectItems container)
        {
            container.AddFromFile(fullPath);
        }
    }
}
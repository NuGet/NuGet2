using System;
using EnvDTE;
using NuGet.VisualStudio.Resources;
using VSLangProj;

namespace NuGet.VisualStudio
{
    public class VsProjectFileProcessingProjectItem : IProjectFileProcessingProjectItem
    {
        private readonly ProjectItem _projectItem;
        public string Path { get; private set; }

        public VsProjectFileProcessingProjectItem(ProjectItem projectItem, string path)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException("projectItem");
            }

            _projectItem = projectItem;
            Path = path;
        }

        public void SetPropertyValue(string name, string value)
        {
            // note: Properties.Item(<name>) throws exception if property is not found
            // http://msdn.microsoft.com/en-us/library/vstudio/envdte.properties.item(v=vs.110).aspx

            try
            {
                var property = _projectItem.Properties.Item(name);
                property.Value = value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(VsResources.ProjectFileProcessor_PropertyNotFound, name, value, _projectItem.Name), ex);
            }
        }

        public void RunCustomTool()
        {
            // run the custom tool
            var vsProjectItem = (VSProjectItem)_projectItem.Object;
            vsProjectItem.RunCustomTool();
        }
    }
}
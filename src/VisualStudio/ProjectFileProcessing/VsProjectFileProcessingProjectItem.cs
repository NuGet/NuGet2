using System;
using EnvDTE;
using VSLangProj;

namespace NuGet.VisualStudio
{
    public class VsProjectFileProcessingProjectItem:
        IProjectFileProcessingProjectItem
    {
        readonly ProjectItem _projectItem;

        public VsProjectFileProcessingProjectItem(ProjectItem projectItem)
        {
            if (projectItem == null) throw new ArgumentNullException("projectItem");
            if (projectItem.FileCount == 0)
                throw new ArgumentException("projectItem", "No files associted with project item");

            _projectItem = projectItem;
        }

        public string Path { get { return _projectItem.FileNames[0]; } }

        public void SetPropertyValue(string name, string value)
        {
            // note: Properties.Item(<name>) throws exception if property is not found
            // http://msdn.microsoft.com/en-us/library/vstudio/envdte.properties.item(v=vs.110).aspx

            var property = _projectItem.Properties.Item(name);
            property.Value = value;
        }

        public void RunCustomTool()
        {
            // run the custom tool
            var vsProjectItem = (VSProjectItem)_projectItem.Object;
            vsProjectItem.RunCustomTool();
        }
    }
}
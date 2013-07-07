using System;
using Microsoft.Build.Evaluation;

namespace NuGet.Common
{
    public class MSBuildProjectFileProcessingProjectItem :
        IProjectFileProcessingProjectItem
    {
        readonly ProjectItem _projectItem;

        public MSBuildProjectFileProcessingProjectItem(ProjectItem projectItem)
        {
            _projectItem = projectItem;
        }

        public string Path { get { return _projectItem.EvaluatedInclude; } }

        public void SetPropertyValue(string name, string value)
        {
            throw new NotImplementedException();
        }

        public void RunCustomTool()
        {
            throw new NotImplementedException();
        }
    }
}
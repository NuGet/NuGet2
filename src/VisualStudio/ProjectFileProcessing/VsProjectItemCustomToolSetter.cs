using System;

namespace NuGet.VisualStudio
{
    public class VsProjectItemCustomToolSetter:
        VsProjectItemProcessorBase
    {
        readonly string _customToolName;
        readonly string _customToolNamespace;

        public const string CustomToolPropertyName = "CustomTool";
        public const string CustomToolNamespacePropertyName = "CustomToolNamespace";

        public VsProjectItemCustomToolSetter(
            string matchPattern, 
            string customToolName, 
            string customToolNamespace) : 
                base(matchPattern)
        {
            if (string.IsNullOrWhiteSpace(matchPattern))
                throw new ArgumentException("matchPattern cannot be null, empty or whitespace", "matchPattern");
            if (string.IsNullOrWhiteSpace(customToolName))
                throw new ArgumentException("customTool cannot be null, empty or whitespace", "customToolName");

            _customToolName = customToolName;
            _customToolNamespace = customToolNamespace;
        }

        public string CustomToolName
        {
            get { return _customToolName; }
        }

        public string CustomToolNamespace
        {
            get { return _customToolNamespace; }
        }

        public override void Process(
            IProjectFileProcessingProjectItem projectItem)
        {
            projectItem.SetPropertyValue(CustomToolPropertyName, CustomToolName);
            projectItem.SetPropertyValue(CustomToolNamespacePropertyName, CustomToolNamespace);

            projectItem.RunCustomTool();
        }
    }
}
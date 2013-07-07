using System;

namespace NuGet.VisualStudio
{
    public class VsProjectItemCustomToolSetter:
        VsProjectItemProcessorBase
    {
        readonly string _customTool;
        readonly string _customToolNamespace;

        public const string CustomToolPropertyName = "CustomTool";
        public const string CustomToolNamespacePropertyName = "CustomToolNamespace";

        public VsProjectItemCustomToolSetter(
            string matchPattern, 
            string customTool, 
            string customToolNamespace) : 
                base(matchPattern)
        {
            if (string.IsNullOrWhiteSpace(matchPattern))
                throw new ArgumentException("matchPattern cannot be null, empty or whitespace", "matchPattern");
            if (string.IsNullOrWhiteSpace(customTool))
                throw new ArgumentException("customTool cannot be null, empty or whitespace", "customTool");

            _customTool = customTool;
            _customToolNamespace = customToolNamespace;
        }

        public override void Process(
            IProjectFileProcessingProjectItem projectItem)
        {
            projectItem.SetPropertyValue(CustomToolPropertyName, _customTool);
            projectItem.SetPropertyValue(CustomToolNamespacePropertyName, _customToolNamespace);

            projectItem.RunCustomTool();
        }
    }
}
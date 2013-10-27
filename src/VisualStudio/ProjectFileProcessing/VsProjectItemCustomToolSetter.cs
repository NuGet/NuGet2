using System;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// A project item processor that handles both the CustomTool and CustomToolNamespace, running the custom tool after setting the properties.
    /// </summary>
    public class VsProjectItemCustomToolSetter : VsProjectItemProcessorBase
    {
        readonly string _customToolName;
        readonly string _customToolNamespace;

        public const string CustomToolPropertyName = "customtool";
        public const string CustomToolNamespacePropertyName = "customtoolnamespace";

        public VsProjectItemCustomToolSetter(string matchPattern, string customToolName, string customToolNamespace)
            : base(matchPattern)
        {
            if (string.IsNullOrWhiteSpace(matchPattern))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "matchPattern");
            }

            if (string.IsNullOrWhiteSpace(customToolName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "customToolName");
            }

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

        public override void Process(IProjectFileProcessingProjectItem projectItem)
        {
            projectItem.SetPropertyValue(CustomToolPropertyName, CustomToolName);
            projectItem.SetPropertyValue(CustomToolNamespacePropertyName, CustomToolNamespace);

            projectItem.RunCustomTool();
        }
    }
}
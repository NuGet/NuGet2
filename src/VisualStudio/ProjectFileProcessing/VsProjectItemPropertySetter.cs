namespace NuGet.VisualStudio
{
    /// <summary>
    /// Sets a property on a VS ProjectItem
    /// </summary>
    public class VsProjectItemPropertySetter : VsProjectItemProcessorBase
    {
        public VsProjectItemPropertySetter(string matchPattern, string propertyName, string propertyValue) :
            base(matchPattern)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
        }

        public string PropertyName { get; private set; }
        public string PropertyValue { get; private set; }

        public override void Process(IProjectFileProcessingProjectItem projectItem)
        {
            projectItem.SetPropertyValue(PropertyName, PropertyValue);
        }
    }
}
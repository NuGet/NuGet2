namespace NuGet.VisualStudio
{
    /// <summary>
    ///     <para>Sets a property on a VS ProjectItem</para>
    /// </summary>
    public class VsProjectItemPropertySetter :
        VsProjectItemProcessorBase
    {
        readonly string _propertyName; 
        readonly string _propertyValue;

        public VsProjectItemPropertySetter(
            string matchPattern,
            string propertyName, string propertyValue):
            base(matchPattern)
        {
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        public string PropertyName
        {
            get { return _propertyName; }
        }

        public string PropertyValue
        {
            get { return _propertyValue; }
        }

        public override void Process(
            IProjectFileProcessingProjectItem projectItem)
        {
            projectItem.SetPropertyValue(PropertyName, PropertyValue);
        }
    }
}
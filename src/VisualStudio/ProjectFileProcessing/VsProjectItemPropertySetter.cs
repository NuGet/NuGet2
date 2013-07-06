using System.Linq;

namespace NuGet.VisualStudio
{
    /// <summary>
    ///     <para>Sets a property on a VS ProjectItem</para>
    /// </summary>
    public class VsProjectItemPropertySetter :
        IProjectFileProcessor
    {
        readonly string _matchPattern;
        readonly string _propertyName;
        readonly string _propertyValue;

        public VsProjectItemPropertySetter(
            string matchPattern,
            string propertyName, string propertyValue)
        {
            _matchPattern = matchPattern;
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        public bool IsMatch(IProjectFileProcessingProjectItem projectItem)
        {
            return PathResolver
                .GetMatches(new[] {projectItem.Path}, p => p, new[] {_matchPattern})
                .Any();
        }

        public void Process(IProjectFileProcessingProjectItem projectItem)
        {
            projectItem.SetPropertyValue(_propertyName, _propertyValue);
        }
    }
}
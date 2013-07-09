using System.Linq;

namespace NuGet.VisualStudio
{
    public abstract class VsProjectItemProcessorBase:
        IProjectFileProcessor
    { 
        readonly string _matchPattern;

        protected VsProjectItemProcessorBase(string matchPattern)
        {
            _matchPattern = matchPattern;
        }

        public string MatchPattern
        {
            get { return _matchPattern; }
        }

        public bool IsMatch(IProjectFileProcessingProjectItem projectItem)
        {
            return PathResolver
                .GetMatches(new[] { projectItem.Path }, p => p, new[] { MatchPattern })
                .Any();
        }

        public abstract void Process(IProjectFileProcessingProjectItem projectItem);
    }
}
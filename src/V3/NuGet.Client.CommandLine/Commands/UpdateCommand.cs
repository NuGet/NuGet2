using NuGet.Common;
using System.Collections.Generic;


namespace NuGet.Commands
{
    [Command(typeof(NuGetCommandResourceType), "update", "UpdateCommandDescription", UsageSummary = "<packages.config|solution|project>",
       UsageExampleResourceName = "UpdateCommandUsageExamples")]
    public class UpdateCommand : Command
    {
        private readonly List<string> _sources = new List<string>();
        [Option(typeof(NuGetCommandResourceType), "UpdateCommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }
        public override void ExecuteCommand()
        {
            var selfUpdater = new SelfUpdater(SourceProvider, Source)
            {
                Console = Console
            };

            selfUpdater.UpdateSelf();
        }   
    }
}

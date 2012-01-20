namespace NuGet.Commands
{
    [Command(typeof(NuGetResources), "publish", "PublishCommandDescription",
        MinArgs = 2, MaxArgs = 3, UsageDescriptionResourceName = "PublishCommandUsageDescription",
        UsageSummaryResourceName = "PublishCommandUsageSummary", UsageExampleResourceName = "PublishCommandUsageExamples")]
    public class PublishCommand : Command
    {
        [Option(typeof(NuGetResources), "PublishCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        public override void ExecuteCommand()
        {
            Console.WriteWarning(NuGetResources.Warning_PublishPackageDeprecated);
        }
    }
}
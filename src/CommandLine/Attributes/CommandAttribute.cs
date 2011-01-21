using System;

namespace NuGet {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CommandAttribute : Attribute {
        public string AltName { get; set; }
        public string CommandName { get; set; }
        public string Description { get; set; }
        public string DescriptionResourceName { get; set; }
        public int MinArgs { get; set; }
        public int MaxArgs { get; set; }
        public string UsageSummary { get; set; }
        public string UsageSummaryResourceName { get; set; }
        public string UsageDescription { get; set; }
        public string UsageDescriptionResourceName { get; set; }

        public Type ResourceType { get; set; }

        public string GetDescription() {
            if (ResourceType != null && !String.IsNullOrEmpty(DescriptionResourceName)) {
                return CommandLineUtility.GetLocalizedString(ResourceType, DescriptionResourceName);
            }
            return Description;
        }

        public string GetUsageSummary() {
            if (ResourceType != null && !String.IsNullOrEmpty(UsageSummaryResourceName)) {
                return CommandLineUtility.GetLocalizedString(ResourceType, UsageSummaryResourceName);
            }
            return UsageSummary;
        }

        public string GetUsageDescription() {
            if (ResourceType != null && !String.IsNullOrEmpty(UsageDescriptionResourceName)) {
                return CommandLineUtility.GetLocalizedString(ResourceType, UsageDescriptionResourceName);
            }
            return UsageDescription;
        }

        public CommandAttribute(string commandName, string description) {
            CommandName = commandName;
            Description = description;
            MinArgs = 0;
            MaxArgs = Int32.MaxValue;
        }

        public CommandAttribute(Type resourceType, string commandName, string descriptionResourceName) {
            ResourceType = resourceType;
            CommandName = commandName;
            DescriptionResourceName = descriptionResourceName;
            MinArgs = 0;
            MaxArgs = Int32.MaxValue;

        }

    }
}

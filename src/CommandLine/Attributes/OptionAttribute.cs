using System;

namespace NuGet {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OptionAttribute : Attribute {
        public string AltName { get; set; }
        public string Description { get; set; }
        public string DescriptionResourceName { get; set; }

        public Type ResourceType { get; set; }

        public string GetDescription() {
            if (ResourceType != null && !String.IsNullOrEmpty(DescriptionResourceName)) {
                return CommandLineUtility.GetLocalizedString(ResourceType, DescriptionResourceName);
            }
            return Description;
        }

        public OptionAttribute(string description) {
            Description = description;
        }

        public OptionAttribute(Type resourceType, string descriptionResourceName) {
            ResourceType = resourceType;
            DescriptionResourceName = descriptionResourceName;
        }
    }
}

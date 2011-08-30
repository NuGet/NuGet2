using System;
using Microsoft.Internal.Web.Utils;

namespace NuGet {
    public class PackageIssue {
        public PackageIssue(string title, string description, string solution) {
            if (String.IsNullOrEmpty(title)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "title");
            }

            if (String.IsNullOrEmpty(description)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "description");
            }

            Title = title;
            Description = description;
            Solution = solution;
        }

        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Solution { get; private set; }

        public override string ToString() {
            return Title;
        }
    }
}
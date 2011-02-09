using System;

namespace NuGetConsole.Implementation {
    public static class GuidList {
        // GUID for the Package Manager Console category in the Font and Colors options page
        public const string GuidPackageManagerConsoleFontAndColorCategoryString = "{F9D6BCE6-C669-41DB-8EE7-DD953828685B}";
        internal static readonly Guid guidPackageManagerConsoleFontAndColorCategory = new Guid(GuidPackageManagerConsoleFontAndColorCategoryString);
    }
}

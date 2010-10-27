namespace NuGet.VisualStudio {
    internal static class VsConstants {
        internal const string WebSiteProjectKind = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        internal const string CsharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        internal const string VbProjectKind = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        
        // All unloaded projects have this Kind value
        internal const string UnloadedProjectKind = "{67294A52-A4F0-11D2-AA88-00C04F688DDE}";

        // Copied from EnvDTE.Constants since that type can't be embedded
        internal const string VsProjectItemKindPhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";
        internal const string VsProjectItemKindPhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
    }
}

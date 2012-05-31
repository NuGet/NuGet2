namespace NuGet
{
    /// <summary>
    /// Set of known context values to be used in calls to HttpUtility.CreateUserAgentString
    /// </summary>
    public static class RepositoryOperationNames
    {
        public static readonly string OperationHeaderName = "NuGet-Operation";

        public static readonly string Update = "Update";
        public static readonly string Install = "Install";
        public static readonly string Restore = "Restore";
    }
}

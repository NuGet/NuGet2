namespace NuPackConsole.Host
{
    /// <summary>
    /// Simple path expansion interface. CommandExpansion tries path expansion
    /// if tab expansion returns no result.
    /// </summary>
    interface IPathExpansion : ITabExpansion
    {
        SimpleExpansion GetPathExpansions(string line);
    }
}

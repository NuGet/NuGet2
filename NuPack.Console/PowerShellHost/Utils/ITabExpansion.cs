namespace NuPackConsole.Host
{
    /// <summary>
    /// Simple (line, lastWord) based tab expansion interface. A host can implement
    /// this interface and reuse CommandExpansion/CommandExpansionProvider.
    /// </summary>
    interface ITabExpansion
    {
        string[] GetExpansions(string line, string lastWord);
    }
}

namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsNoItemsMessageTreeNode : IVsExtensionsTreeNode
    {
        string NoItemsMessage { get; }
    }
}


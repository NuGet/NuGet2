namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsExtensionsProviderSelectionCallback
    {
        void NotifyNodeSelected(IVsExtensionsTreeNode node);
    }
}


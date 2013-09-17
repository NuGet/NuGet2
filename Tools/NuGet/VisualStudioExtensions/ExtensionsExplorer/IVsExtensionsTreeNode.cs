namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public interface IVsExtensionsTreeNode
    {
        IList Extensions { get; }

        bool IsExpanded { get; set; }

        bool IsSearchResultsNode { get; }

        bool IsSelected { get; set; }

        string Name { get; }

        IList<IVsExtensionsTreeNode> Nodes { get; }

        IVsExtensionsTreeNode Parent { get; }
    }
}


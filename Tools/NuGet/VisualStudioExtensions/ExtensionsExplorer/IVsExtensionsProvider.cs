namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsExtensionsProvider
    {
        IVsExtensionsTreeNode Search(string searchTerms);

        object DetailViewDataTemplate { get; }

        IVsExtensionsTreeNode ExtensionsTree { get; }

        object HeaderContent { get; }

        object ItemContainerStyle { get; }

        object LargeIconDataTemplate { get; }

        bool ListMultiSelect { get; }

        bool ListVisibility { get; }

        object MediumIconDataTemplate { get; }

        string Name { get; }

        object SmallIconDataTemplate { get; }

        float SortOrder { get; }

        object View { get; }
    }
}


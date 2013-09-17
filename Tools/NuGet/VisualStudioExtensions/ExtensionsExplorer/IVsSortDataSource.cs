namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;
    using System.Collections.Generic;

    public interface IVsSortDataSource
    {
        IList<IVsSortDescriptor> GetSortDescriptors();
        bool SortSelectionChanged(IVsSortDescriptor selectedDescriptor);
    }
}


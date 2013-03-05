namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;
    using System.Collections;

    public interface IVsSortDescriptor : IComparer
    {
        string Name { get; }
    }
}


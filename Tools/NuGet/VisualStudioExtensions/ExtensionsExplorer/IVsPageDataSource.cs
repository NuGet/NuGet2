namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsPageDataSource
    {
        event EventHandler<EventArgs> PageDataChanged;

        void LoadPage(int pageNumber);

        int CurrentPage { get; }

        int TotalPages { get; }
    }
}


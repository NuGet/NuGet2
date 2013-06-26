namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsProgressPaneConsumer
    {
        void SetProgressPane(IVsProgressPane progressPane);
    }
}


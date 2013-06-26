namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsMessagePaneConsumer
    {
        void SetMessagePane(IVsMessagePane messagePane);
    }
}


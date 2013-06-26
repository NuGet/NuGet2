namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;

    public interface IVsProgressPane
    {
        void Close();
        void RequestCancel();
        void SetMessageThreadSafe(string message);
        void SetProgressThreadSafe(double progress);
        bool Show(CancelProgressCallback cancelCallback, bool showCancelButton);

        bool IsIndeterminate { get; set; }
    }
}


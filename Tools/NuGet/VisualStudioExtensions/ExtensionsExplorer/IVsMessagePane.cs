namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;
    using System.Windows;

    public interface IVsMessagePane
    {
        void Close();
        void SetMessageThreadSafe(string message);
        void SetMessageThreadSafe(string message, string linkText, RoutedEventHandler linkTextClickHandler);
        bool Show();
    }
}


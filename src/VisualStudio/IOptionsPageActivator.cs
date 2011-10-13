using System;

namespace NuGet.VisualStudio
{
    public interface IOptionsPageActivator
    {
        void NotifyOptionsDialogClosed();
        void ActivatePage(OptionsPage page, Action closeCallback);
    }
}
using System;

namespace Microsoft.VisualStudio.Project
{
    public interface IDirectAccessService
    {
        void Write(string fileToWrite, Action<IDirectWriteAccess> action, ProjectAccess flags = ProjectAccess.None);
    }
}

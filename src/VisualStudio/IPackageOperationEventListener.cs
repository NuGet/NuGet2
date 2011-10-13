using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface IPackageOperationEventListener
    {
        void OnBeforeAddPackageReference(Project project);
        void OnAfterAddPackageReference(Project project);
        void OnAddPackageReferenceError(Project project, Exception exception);
    }
}
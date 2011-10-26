using System;

namespace NuGet.VisualStudio
{
    internal sealed class NullPackageOperationEventListener : IPackageOperationEventListener
    {
        public static readonly NullPackageOperationEventListener Instance = new NullPackageOperationEventListener();

        private NullPackageOperationEventListener()
        {
        }

        public void OnBeforeAddPackageReference(EnvDTE.Project project)
        {
        }

        public void OnAfterAddPackageReference(EnvDTE.Project project)
        {
        }

        public void OnAddPackageReferenceError(EnvDTE.Project project, Exception exception)
        {
        }

        public void OnBeforePackageOperation(IVsPackageManager packageManager)
        {
        }

        public void OnAfterPackageOperation(IVsPackageManager packageManager)
        {
        }
    }
}

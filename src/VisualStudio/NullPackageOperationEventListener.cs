using System;

namespace NuGet.VisualStudio
{
    internal sealed class NullPackageOperationEventListener : IPackageOperationEventListener
    {
        public static readonly NullPackageOperationEventListener Instance = new NullPackageOperationEventListener();

        private NullPackageOperationEventListener()
        {
        }

        public void OnBeforeAddPackageReference(IProjectManager projectManager)
        {
        }

        public void OnAfterAddPackageReference(IProjectManager projectManager)
        {
        }

        public void OnAddPackageReferenceError(IProjectManager projectManager, Exception exception)
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

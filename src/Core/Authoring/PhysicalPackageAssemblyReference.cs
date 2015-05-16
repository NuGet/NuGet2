using System;
using System.IO;

namespace NuGet
{
    public class PhysicalPackageAssemblyReference : PhysicalPackageFile, IPackageAssemblyReference
    {
        public PhysicalPackageAssemblyReference(bool useManagedCodeConventions)
            : base(useManagedCodeConventions)
        {
        }

        public PhysicalPackageAssemblyReference(PhysicalPackageFile file, bool useManagedCodeConventions)
            : base(file, useManagedCodeConventions)
        {

        }

        public PhysicalPackageAssemblyReference(Func<Stream> streamFactory, bool useManagedCodeConventions)
            : base(streamFactory, useManagedCodeConventions)
        {
        }

        public string Name
        {
            get 
            {
                return String.IsNullOrEmpty(Path) ? String.Empty : System.IO.Path.GetFileName(Path);
            }
        }
    }
}
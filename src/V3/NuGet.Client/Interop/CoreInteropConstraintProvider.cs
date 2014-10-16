using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client.Interop
{
    internal class CoreInteropConstraintProvider : IPackageConstraintProvider
    {
        private InstalledPackagesList _installed;

        public CoreInteropConstraintProvider(InstalledPackagesList installed)
        {
            _installed = installed;
        }

        public string Source
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IVersionSpec GetConstraint(string packageId)
        {
            var reference = _installed.GetInstalledPackage(packageId);
            if (reference == null)
            {
                return null;
            }
            return CoreConverters.SafeToVerSpec(reference.VersionConstraint);
        }
    }
}

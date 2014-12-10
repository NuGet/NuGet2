using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.V3Interop;
using NuGet.Client;

namespace NuGet.Client.Interop
{
    internal class CoreInteropSourceRepository : IPackageRepository, IV3InteropRepository
    {
        private SourceRepository _source;

        public CoreInteropSourceRepository(SourceRepository source)
        {
            _source = source;
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return _source.GetPackageMetadataById(packageId).Result.Select(PackageJsonLd.PackageFromJson);
        }

        #region Unimplemented stuff
        public string Source
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public PackageSaveModes PackageSaveMode
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
            set
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
        }

        public bool SupportsPrereleasePackages
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
        }

        public IQueryable<IPackage> GetPackages()
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public void AddPackage(IPackage package)
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public void RemovePackage(IPackage package)
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }
        #endregion
    }
}

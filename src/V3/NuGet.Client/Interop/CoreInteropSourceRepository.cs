using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.V3Interop;

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
            get { throw new NotImplementedException(); }
        }

        public PackageSaveModes PackageSaveMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool SupportsPrereleasePackages
        {
            get { throw new NotImplementedException(); }
        }

        public IQueryable<IPackage> GetPackages()
        {
            throw new NotImplementedException();
        }

        public void AddPackage(IPackage package)
        {
            throw new NotImplementedException();
        }

        public void RemovePackage(IPackage package)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

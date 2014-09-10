using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client.Interop
{
    internal class V3InteropLocalRepository : IPackageRepository
    {
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
    }
}

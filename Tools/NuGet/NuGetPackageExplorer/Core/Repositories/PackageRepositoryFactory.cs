using System;
using System.Globalization;
using NuGet.Resources;
using NuGet.Repositories;
using System.Net;
using NuGet.Utility;

namespace NuGet {
    public class PackageRepositoryFactory {
        private static readonly PackageRepositoryFactory _default = new PackageRepositoryFactory();

        public static PackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public virtual IPackageRepository CreateRepository(IHttpClient repositoryClient)
        {
            if (null == repositoryClient)
            {
                throw new ArgumentNullException("repositoryClient");
            }
            return new DataServicePackageRepository(repositoryClient);
        }
    }
}

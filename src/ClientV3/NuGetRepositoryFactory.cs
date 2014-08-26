using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.VisualStudio.ClientV3.Interop;

namespace NuGet.VisualStudio.ClientV3
{
    public interface INuGetRepositoryFactory
    {
        INuGetRepository Create(PackageSource targetSource);
    }

    public class NuGetRepositoryFactory : INuGetRepositoryFactory
    {
        public static readonly NuGetRepositoryFactory Default = new NuGetRepositoryFactory(PackageRepositoryFactory.Default);

        private readonly IPackageRepositoryFactory _v2RepoFactory;
        
        public NuGetRepositoryFactory(IPackageRepositoryFactory v2RepoFactory)
        {
            _v2RepoFactory = v2RepoFactory;
        }

        public INuGetRepository Create(PackageSource targetSource)
        {
            return Create(_v2RepoFactory.CreateRepository(targetSource.Source));
        }

        private INuGetRepository Create(IPackageRepository packageRepository)
        {
            return new V2InteropRepository(packageRepository);
        }
    }
}

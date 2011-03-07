using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ninject;
using NuGet.Server.DataServices;

namespace NuGet.Server.Infrastructure {
    public class ServerPackageRepository : LocalPackageRepository, IServerPackageRepository {
        private readonly IDictionary<IPackage, DerivedPackageData> _derivedDataLookup = new Dictionary<IPackage, DerivedPackageData>();

        public ServerPackageRepository(string path)
            : base(path) {
        }

        public ServerPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : base(pathResolver, fileSystem) {
        }

        [Inject]
        public IHashProvider HashProvider { get; set; }

        public IQueryable<Package> GetPackagesWithDerivedData() {
            return from package in base.GetPackages()
                   select new Package(package, _derivedDataLookup[package]);
        }

        protected override IPackage OpenPackage(string path) {
            IPackage package = base.OpenPackage(path);
            _derivedDataLookup[package] = CalculateDerivedData(FileSystem.GetFullPath(path));
            return package;
        }

        private DerivedPackageData CalculateDerivedData(string path) {
            byte[] fileBytes;
            using (Stream stream = FileSystem.OpenFile(path)) {
                fileBytes = stream.ReadAllBytes();
            }

            return new DerivedPackageData {
                PackageSize = fileBytes.Length,
                PackageHash = Convert.ToBase64String(HashProvider.CalculateHash(fileBytes)),
                LastUpdated = FileSystem.GetLastModified(path),
                Created = FileSystem.GetCreated(path)
            };
        }
    }
}

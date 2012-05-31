using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NuGet.Test.Mocks
{
    public class MockPackageRepository : PackageRepositoryBase, ICollection<IPackage>, ILatestPackageLookup, IOperationAwareRepository
    {
        private readonly string _source;

        public string LastOperation { get; private set; }

        public MockPackageRepository()
            : this("")
        {
        }

        public MockPackageRepository(string source)
        {
            Packages = new Dictionary<string, List<IPackage>>();
            _source = source;
        }

        public override string Source
        {
            get
            {
                return _source;
            }
        }

        public override bool SupportsPrereleasePackages
        {
            get
            {
                return true;
            }
        }

        internal Dictionary<string, List<IPackage>> Packages
        {
            get;
            set;
        }

        public override void AddPackage(IPackage package)
        {
            AddPackage(package.Id, package);
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return Packages.Values.SelectMany(p => p).AsQueryable();
        }

        public override void RemovePackage(IPackage package)
        {
            List<IPackage> packages;
            if (Packages.TryGetValue(package.Id, out packages))
            {
                packages.Remove(package);
            }

            if (packages.Count == 0)
            {
                Packages.Remove(package.Id);
            }
        }

        private void AddPackage(string id, IPackage package)
        {
            List<IPackage> packages;
            if (!Packages.TryGetValue(id, out packages))
            {
                packages = new List<IPackage>();
                Packages.Add(id, packages);
            }
            packages.Add(package);
        }

        public void Add(IPackage item)
        {
            AddPackage(item);
        }

        public void Clear()
        {
            Packages.Clear();
        }

        public bool Contains(IPackage item)
        {
            return this.Exists(item);
        }

        public void CopyTo(IPackage[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public int Count
        {
            get
            {
                return GetPackages().Count();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Remove(IPackage item)
        {
            if (this.Exists(item))
            {
                RemovePackage(item);
                return true;
            }
            return false;
        }

        public IEnumerator<IPackage> GetEnumerator()
        {
            return GetPackages().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryFindLatestPackageById(string id, out SemanticVersion latestVersion)
        {
            List<IPackage> packages;
            bool result = Packages.TryGetValue(id, out packages);
            if (result && packages.Count > 0)
            {
                packages.Sort((a, b) => b.Version.CompareTo(a.Version));
                latestVersion = packages[0].Version;
                return true;
            }
            else
            {
                latestVersion = null;
                return false;
            }
        }

        public IDisposable StartOperation(string operation)
        {
            LastOperation = null;
            return new DisposableAction(() => { LastOperation = operation; });
        }
    }
}

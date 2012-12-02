using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NuGet.Test.Mocks
{
    public class MockSharedPackageRepository : MockPackageRepository, ISharedPackageRepository
    {
        private List<Tuple<string, SemanticVersion>> _references;

        public MockSharedPackageRepository()
            : this("")
        {
        }

        public MockSharedPackageRepository(string source) : base(source)
        {
            _references = new List<Tuple<string, SemanticVersion>>();
        }
        
        public bool IsReferenced(string packageId, SemanticVersion version)
        {
            return _references.Any(r => r.Item1 == packageId && r.Item2 == version);
        }

        public void AddPackageReferenceEntry(string packageId, SemanticVersion version)
        {
            _references.Add(Tuple.Create(packageId, version));
        }

        public void RegisterRepository(string path)
        {
            throw new NotImplementedException();
        }

        public void UnregisterRepository(string path)
        {
            throw new NotImplementedException();
        }
    }
}

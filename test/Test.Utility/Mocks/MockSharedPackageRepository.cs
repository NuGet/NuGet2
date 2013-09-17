using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NuGet.Test.Mocks
{
    public class MockSharedPackageRepository : MockPackageRepository, ISharedPackageRepository
    {
        private Dictionary<string, SemanticVersion> _references = 
            new Dictionary<string, SemanticVersion>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, SemanticVersion> _solutionReferences = 
            new Dictionary<string, SemanticVersion>(StringComparer.OrdinalIgnoreCase);

        public MockSharedPackageRepository()
            : this("")
        {
        }

        public MockSharedPackageRepository(string source) : base(source)
        {
        }

        public override void AddPackage(IPackage package)
        {
            base.AddPackage(package);

            if (package.HasProjectContent())
            {
                _references[package.Id] = package.Version;
            }
            else
            {
                _solutionReferences[package.Id] = package.Version;
            }
        }

        public override void RemovePackage(IPackage package)
        {
            base.RemovePackage(package);

            if (package.HasProjectContent())
            {
                _references.Remove(package.Id);
            }
            else
            {
                _solutionReferences.Remove(package.Id);
            }
        }
        
        public bool IsReferenced(string packageId, SemanticVersion version)
        {
            SemanticVersion storedVersion;
            return _references.TryGetValue(packageId, out storedVersion) && storedVersion == version;
        }

        public bool IsSolutionReferenced(string packageId, SemanticVersion version)
        {
            SemanticVersion storedVersion;
            return _solutionReferences.TryGetValue(packageId, out storedVersion) && storedVersion == version;
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public class PackageReferenceSet : IFrameworkTargetable
    {
        private readonly FrameworkName _targetFramework;
        private readonly ICollection<string> _references;
        private readonly ReadOnlyCollection<PackageProperty> _properties;

        public PackageReferenceSet(FrameworkName targetFramework, IEnumerable<string> references)
            : this(targetFramework, references, properties: null)
        {
        }

        public PackageReferenceSet(FrameworkName targetFramework, IEnumerable<string> references, IEnumerable<PackageProperty> properties)
        {
            if (references == null)
            {
                throw new ArgumentNullException("references");
            }

            _targetFramework = targetFramework;
            _references = new ReadOnlyCollection<string>(references.ToList());

            // Properties are optional - so we'll create an empty list by default
            _properties = new ReadOnlyCollection<PackageProperty>(properties != null ? properties.ToList() : new List<PackageProperty>(0));
        }

        public PackageReferenceSet(ManifestReferenceSet manifestReferenceSet)
        {
            if (manifestReferenceSet == null) 
            {
                throw new ArgumentNullException("manifestReferenceSet");
            }

            if (!String.IsNullOrEmpty(manifestReferenceSet.TargetFramework))
            {
                _targetFramework = VersionUtility.ParseFrameworkName(manifestReferenceSet.TargetFramework);
            }

            _references = new ReadOnlyHashSet<string>(manifestReferenceSet.References.Select(r => r.File), StringComparer.OrdinalIgnoreCase);
            _properties = new ReadOnlyCollection<PackageProperty>(manifestReferenceSet.Properties != null ? manifestReferenceSet.Properties.Select(p => new PackageProperty(p.Name, p.Value)).ToList() : new List<PackageProperty>(0));
        }

        public ICollection<string> References
        {
            get
            {
                return _references;
            }
        }

        public IEnumerable<PackageProperty> Properties
        {
            get
            {
                return _properties;
            }
        }

        public FrameworkName TargetFramework
        {
            get { return _targetFramework; }
        }

        public IEnumerable<FrameworkName> SupportedFrameworks
        {
            get
            {
                if (TargetFramework == null)
                {
                    yield break;
                }

                yield return TargetFramework;
            }
        }
    }
}
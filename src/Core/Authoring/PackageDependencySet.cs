using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public class PackageDependencySet : IFrameworkTargetable
    {
        private readonly FrameworkName _targetFramework;
        private readonly ReadOnlyCollection<PackageDependency> _dependencies;
        private readonly ReadOnlyCollection<PackageProperty> _properties;

        public PackageDependencySet(FrameworkName targetFramework, IEnumerable<PackageDependency> dependencies)
            : this(targetFramework, dependencies, properties: null)
        {
        }

        public PackageDependencySet(FrameworkName targetFramework, IEnumerable<PackageDependency> dependencies, IEnumerable<PackageProperty> properties)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException("dependencies");
            }

            _targetFramework = targetFramework;
            _dependencies = new ReadOnlyCollection<PackageDependency>(dependencies.ToList());

            // Properties are optional - so we'll create an empty list by default
            _properties = new ReadOnlyCollection<PackageProperty>(properties != null ? properties.ToList() : new List<PackageProperty>(0));
        }

        public FrameworkName TargetFramework
        {
            get
            {
                return _targetFramework;
            }
        }

        public IEnumerable<PackageProperty> Properties
        {
            get
            {
                return _properties;
            }
        }

        public ICollection<PackageDependency> Dependencies
        {
            get
            {
                return _dependencies;
            }
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
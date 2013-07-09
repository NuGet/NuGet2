using System.Collections.Generic;
using System.Linq;

namespace NuGet
{
    public class PackageManifestFile:IPackageManifestFile
    {
        readonly string _source;
        readonly string _target;
        readonly string _exclude; 
        readonly IEnumerable<IPackageManifestFileProperty> _properties;

        internal PackageManifestFile(ManifestFile file)
        {
            _source = file.Source;
            _target = file.Target;
            _exclude = file.Exclude;
            _properties = file.Properties
                              .Select(property => new PackageManifestFileProperty(property))
                              .ToArray();
        }

        public string Source
        {
            get { return _source; }
        }

        public string Target
        {
            get { return _target; }
        }

        public string Exclude
        {
            get { return _exclude; }
        }

        public IEnumerable<IPackageManifestFileProperty> Properties
        {
            get { return _properties; }
        }
    }
}
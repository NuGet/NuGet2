using NuGet.Authoring;

namespace NuGet
{
    public class PackageManifestFileProperty:IPackageManifestFileProperty
    {
        readonly string _name;
        readonly string _value;

        internal PackageManifestFileProperty(ManifestFileProperty property)
        {
            _name = property.Name;
            _value = property.Value;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Value
        {
            get { return _value; }
        }
    }
}
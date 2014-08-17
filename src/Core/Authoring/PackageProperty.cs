
namespace NuGet
{
    public class PackageProperty
    {
        public PackageProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }
    }
}

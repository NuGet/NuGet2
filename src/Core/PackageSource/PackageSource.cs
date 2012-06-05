using System;
using System.Runtime.Serialization;

namespace NuGet
{
    [DataContract]
    public class PackageSource : IEquatable<PackageSource>
    {
        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string Source { get; private set; }

        public bool IsOfficial { get; set; }

        public bool IsEnabled { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
         
        public PackageSource(string source) :
            this(source, source, isEnabled: true)
        {
        }

        public PackageSource(string source, string name) :
            this(source, name, isEnabled: true)
        {
        }

        public PackageSource(string source, string name, bool isEnabled)
            : this(source, name, isEnabled, isOfficial: false)
        {
        }

        public PackageSource(string source, string name, bool isEnabled, bool isOfficial)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
            Source = source;
            IsEnabled = isEnabled;
            IsOfficial = isOfficial;
        }

        public bool Equals(PackageSource other)
        {
            if (other == null)
            {
                return false;
            }

            return Name.Equals(other.Name, StringComparison.CurrentCultureIgnoreCase) &&
                Source.Equals(other.Source, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            var source = obj as PackageSource;
            if (source != null)
            {
                return Equals(source);
            }
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return Name + " [" + Source + "]";
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() * 3137 + Source.GetHashCode();
        }

        public PackageSource Clone()
        {
            return new PackageSource(Source, Name, IsEnabled, IsOfficial) { UserName = UserName, Password = Password };
        }
    }
}
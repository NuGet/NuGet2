using System;
using System.Runtime.Serialization;

namespace NuGet {

    [DataContract]
    public class PackageSource : IEquatable<PackageSource> {

        [DataMember]
        public bool IsAggregate { get; set; }

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string Source { get; private set; }

        public PackageSource(string source) 
            : this(source, source) {
        }

        public PackageSource(string source, string name) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (name == null) {
                throw new ArgumentNullException("name");
            }

            Name = name;
            Source = source;
        }

        public static implicit operator PackageSource(string source) {
            return new PackageSource(source);
        }

        public bool Equals(PackageSource other) {
            if (other == null) {
                return false;
            }

            return Name.Equals(other.Name, StringComparison.CurrentCultureIgnoreCase) &&
                Source.Equals(other.Source, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            var source = obj as PackageSource;
            if (obj != null) {
                return Equals(source);
            }
            return (obj == null) && base.Equals(obj);
        }

        public override string ToString() {
            return Name + " [" + Source + "]";
        }

        public override int GetHashCode() {
            return Name.GetHashCode() * 3137 + Source.GetHashCode();
        }
    }
}

﻿using System;
using System.Runtime.Serialization;

namespace NuPack {

    [DataContract]
    public class PackageSource : IEquatable<PackageSource> {

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string Source { get; private set; }

        public PackageSource(string name, string source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (name == null) {
                throw new ArgumentNullException("name");
            }

            this.Name = name;
            this.Source = source;
        }

        public bool Equals(PackageSource other) {
            return Name.Equals(other.Name, StringComparison.CurrentCultureIgnoreCase) &&
                Source.Equals(other.Source, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() {
            return Name + " [" + Source + "]";
        }

        public override int GetHashCode() {
            return Name.GetHashCode() ^ Source.GetHashCode();
        }
    }
}
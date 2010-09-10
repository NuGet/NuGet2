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

            Name = name;
            Source = source;
        }

        public bool Equals(PackageSource other) {
            return Name.Equals(other.Name, StringComparison.CurrentCultureIgnoreCase) &&
                Source.Equals(other.Source, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return base.Equals(obj);
            }

            if (obj is PackageSource) {
                return Equals(obj as PackageSource);
            }
            else {
                return false;
            }
        }

        public override string ToString() {
            return Name + " [" + Source + "]";
        }

        public override int GetHashCode() {
            return Name.GetHashCode() ^ Source.GetHashCode();
        }
    }
}
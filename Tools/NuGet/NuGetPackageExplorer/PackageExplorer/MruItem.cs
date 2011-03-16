using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackageExplorer
{
    public sealed class MruItem : IEquatable<MruItem>
    {
        public string Path { get; set; }
        public string PackageName { get; set; }
        public PackageType PackageType { get; set; }

        public bool Equals(MruItem other)
        {
            if (other == null)
            {
                return false;
            }
            return Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MruItem);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }

    public enum PackageType
    {
        ZipPackge,
        DataServicePackage
    }
}

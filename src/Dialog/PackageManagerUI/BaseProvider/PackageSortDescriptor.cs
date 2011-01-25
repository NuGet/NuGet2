using System;
using System.ComponentModel;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {
    public class PackageSortDescriptor : IVsSortDescriptor {
        public PackageSortDescriptor(string displayName, string name)
            : this(displayName, name, ListSortDirection.Ascending) {
        }

        public PackageSortDescriptor(string displayName, string name, ListSortDirection direction) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            DisplayName = displayName;
            Name = name;
            Direction = direction;
        }

        public string DisplayName {
            get;
            private set;
        }

        public string Name {
            get;
            private set;
        }

        public ListSortDirection Direction {
            get;
            private set;
        }

        public int Compare(object x, object y) {
            throw new NotSupportedException();
        }

        public override string ToString() {
            return DisplayName ?? Name;
        }
    }

}

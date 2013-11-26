using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    public class PackageSortDescriptor : IVsSortDescriptor
    {
        public PackageSortDescriptor(string displayName, string sortProperty, ListSortDirection direction)
            : this(displayName, new[] { sortProperty }, direction)
        {
        }

        public PackageSortDescriptor(string displayName, IEnumerable<string> sortProperties, ListSortDirection direction)
        {
            if (sortProperties == null || !sortProperties.Any())
            {
                throw new ArgumentNullException("sortProperties");
            }
            DisplayName = displayName;
            SortProperties = sortProperties;
            Direction = direction;
        }

        public string DisplayName
        {
            get;
            private set;
        }

        public IEnumerable<string> SortProperties
        {
            get;
            private set;
        }

        public ListSortDirection Direction
        {
            get;
            private set;
        }

        public string Name
        {
            get { return DisplayName; }
        }

        public int Compare(object x, object y)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return DisplayName ?? String.Join(" ", SortProperties);
        }

    }
}

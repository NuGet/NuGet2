using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Versioning;

namespace NuGet.Client.VisualStudio.UI
{
    public class UiPackageDependency
    {
        public string Id
        {
            get;
            private set;
        }

        public VersionRange Range
        {
            get;
            private set;
        }

        public UiPackageDependency(string id, VersionRange range)
        {
            Id = id;
            Range = range;
        }
    }
}

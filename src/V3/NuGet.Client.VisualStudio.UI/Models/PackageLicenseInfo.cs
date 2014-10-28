using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.UI
{
    public class PackageLicenseInfo
    {
        public PackageLicenseInfo(
            string id,
            Uri licenseUrl,
            string authors)
        {
            Id = id;
            LicenseUrl = licenseUrl;
            Authors = authors;
        }

        public string Id
        {
            get;
            private set;
        }

        public Uri LicenseUrl
        {
            get;
            private set;
        }

        public string Authors
        {
            get;
            private set;
        }
    }
}

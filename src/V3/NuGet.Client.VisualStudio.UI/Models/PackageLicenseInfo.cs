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
            string licenseUrl,
            string author)
        {
            Id = id;
            LicenseUrl = licenseUrl;
            Author = author;
        }

        public string Id
        {
            get;
            private set;
        }

        public string LicenseUrl
        {
            get;
            private set;
        }

        public string Author
        {
            get;
            private set;
        }
    }
}

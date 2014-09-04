using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public class PackageSource
    {
        public string Name { get; private set; }
        public string Url { get; private set; }

        public PackageSource(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}

using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public interface IDownload
    {
      Uri GetNupkgUrlForDownload(string id, NuGetVersion version);
    }
}

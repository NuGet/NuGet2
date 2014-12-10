using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.BaseTypes.Respository
{
    public interface IDownloadResource
    {
      Uri GetNupkgUrlForDownload(string id, NuGetVersion version);
    }
}

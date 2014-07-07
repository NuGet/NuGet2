using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.ShimV3
{
    public interface IShimCache : IDisposable
    {
        void AddOrUpdate(Uri uri, JObject blob);

        bool TryGet(Uri uri, out JObject blob);
    }
}

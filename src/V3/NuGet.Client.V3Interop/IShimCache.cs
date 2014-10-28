using Newtonsoft.Json.Linq;
using System;

namespace NuGet.Client.V3Shim
{
    public interface IShimCache : IDisposable
    {
        void AddOrUpdate(Uri uri, JObject blob);

        bool TryGet(Uri uri, out JObject blob);
    }
}

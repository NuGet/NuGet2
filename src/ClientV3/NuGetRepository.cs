using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.VisualStudio.Client.Interop;

namespace NuGet.VisualStudio.Client
{
    public static class NuGetRepository
    {
        public static INuGetRepository Create(string targetSourceUrl)
        {
            return Create(new Uri(targetSourceUrl));
        }

        public static INuGetRepository Create(Uri targetSource)
        {
            return new V2InteropRepository(targetSource);
        }
    }
}

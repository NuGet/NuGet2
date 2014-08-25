using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // Debug Assert for our code, InvalidOperationException for any other code that calls this.
            Debug.Assert(!String.Equals(targetSourceUrl, "(Aggregate source)", StringComparison.OrdinalIgnoreCase), "Cannot create a NuGet Repository from the Aggregate Source");
            if (String.Equals(targetSourceUrl, "(Aggregate source)", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(Strings.NuGetRepository_CannotCreateAggregateRepo);
            }

            return Create(new Uri(targetSourceUrl));
        }

        public static INuGetRepository Create(Uri targetSource)
        {
            return new V2InteropRepository(targetSource);
        }
    }
}

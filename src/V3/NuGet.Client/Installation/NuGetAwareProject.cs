using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client.Installation
{
    public abstract class NuGetAwareProject
    {
        public abstract Task InstallPackage(
            PackageIdentity id,

            // the supported frameworks of the package
            IEnumerable<FrameworkName> frameworks,

            IExecutionLogger logger,             
            CancellationToken cancelToken);

        public abstract Task UninstallPackage(PackageIdentity id, IExecutionLogger logger, CancellationToken cancelToken);
    }
}

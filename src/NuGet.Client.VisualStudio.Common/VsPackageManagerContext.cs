using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Common
{
    public class VsPackageManagerContext
    {
        public SourceRepositoryProvider Sources { get; private set; }

        public NuGetProject Target { get; private set; }

    }
}

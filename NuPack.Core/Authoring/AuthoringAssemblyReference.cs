using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Versioning;

namespace NuPack {
    public class AuthoringAssemblyReference : AuthoringPackageFile, IPackageAssemblyReference  {
        public FrameworkName TargetFramework {
            get; set;
        }
    }
}

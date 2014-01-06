using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using NuGet;

namespace NuGet.WebMatrix.Tests.Utilities
{
    public static class PackageFactory
    {
        public static IPackage Create(string id)
        {
            return new PackageStub(id);
        }

        public static IPackage Create(string id, Version version)
        {
            return new PackageStub(id, version);
        }

        public static IPackage Create(string id, Version version, IEnumerable<PackageDependency> dependencies)
        {
            return new PackageStub(id, version, dependencies);
        }

        public static IPackage Create(string id, Version version, IEnumerable<PackageDependency> dependencies, IEnumerable<FrameworkName> supportedFrameworks)
        {
            return new PackageStub(id, version, dependencies, supportedFrameworks);
        }
    }
}

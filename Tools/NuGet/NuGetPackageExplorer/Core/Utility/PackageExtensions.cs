using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet {
    public static class PackageExtensions {

        public static string GetFullName(this IPackageMetadata package) {
            return package.Id + " " + package.Version;
        }

    }
}

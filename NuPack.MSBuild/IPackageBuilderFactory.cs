using System;
using NuGet.Authoring;

namespace NuGet.Authoring {
    public interface IPackageBuilderFactory {
        IPackageBuilder CreateFrom(string path);
    }
}

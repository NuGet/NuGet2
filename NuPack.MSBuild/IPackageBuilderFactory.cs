using System;
using NuPack.Authoring;

namespace NuPack.Authoring {
    public interface IPackageBuilderFactory {
        IPackageBuilder CreateFrom(string path);
    }
}
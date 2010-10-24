using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace NuPack.Authoring {
    public interface IPackageBuilder {
        string Id { get; }
        string Version { get; }
        void Save(Stream stream, string basePath);
    }
}

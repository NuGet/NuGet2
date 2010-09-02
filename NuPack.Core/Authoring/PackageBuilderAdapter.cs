using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuPack {
    public class PackageBuilderAdapter {
        private Package _package;

        public PackageBuilderAdapter(Package package) {
            _package = package;
        }

        public void ReadContentTo(PackageBuilder packageBuilder) {
            packageBuilder.Authors.AddRange(_package.Authors);
            packageBuilder.Category = _package.Category;
            packageBuilder.Created = _package.Created;
            packageBuilder.Description = _package.Description;
            packageBuilder.Id = _package.Id;
            packageBuilder.Keywords.AddRange(_package.Keywords);
            packageBuilder.Version = _package.Version;

            packageBuilder.Dependencies.AddRange(_package.Dependencies);

            packageBuilder.References.AddRange(_package.AssemblyReferences);
            packageBuilder.Resources.AddRange(_package.GetFiles(PackageFileType.Resource.ToString()));
            packageBuilder.PackageFiles.AddRange(_package.GetFiles(PackageFileType.Content.ToString()));
        }
    }
}

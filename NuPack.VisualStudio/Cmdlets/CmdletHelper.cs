using System;

namespace NuPack.VisualStudio.Cmdlets {

    internal static class CmdletHelper {

        public static bool IsSolutionOnlyPackage(IPackageRepository repository, string id, Version version = null) {
            var package = repository.FindPackage(id, null, null, version);
            return package != null && !package.HasProjectContent();
        }
    }
}

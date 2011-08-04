using System;
namespace NuGet {
    public class PackageOperation {
        public PackageOperation(IPackage package, PackageAction action) {
            Package = package;
            Action = action;
        }

        public IPackage Package {
            get;
            private set;
        }

        public PackageAction Action {
            get;
            private set;
        }

        public override string ToString() {
            return (Action == PackageAction.Install ? "+ " : "- ") + Package.Id + " " + Package.Version;
        }

        public override bool Equals(object obj) {
            var operation = obj as PackageOperation;
            return operation != null &&
                   operation.Action == Action &&
                   operation.Package.Id.Equals(Package.Id, StringComparison.OrdinalIgnoreCase) &&
                   VersionUtility.NormalizeVersion(operation.Package.Version).Equals(VersionUtility.NormalizeVersion(Package.Version));
        }

        public override int GetHashCode() {
            var combiner = new HashCodeCombiner();
            combiner.AddObject(Package.Id);
            combiner.AddObject(VersionUtility.NormalizeVersion(Package.Version));
            combiner.AddObject(Action);

            return combiner.CombinedHash;
        }
    }
}

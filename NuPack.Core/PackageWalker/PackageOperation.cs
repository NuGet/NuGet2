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
    }
}

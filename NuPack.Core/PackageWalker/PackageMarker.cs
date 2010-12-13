namespace NuGet {
    using System.Collections.Generic;

    public class PackageMarker {
        public PackageMarker()
            : this(PackageEqualityComparer.Id) {
        }

        public PackageMarker(IEqualityComparer<IPackage> equalityComparer) {
            Visited = new Dictionary<IPackage, VisitedState>(equalityComparer);
        }

        private IDictionary<IPackage, VisitedState> Visited {
            get;
            set;
        }

        public IEnumerable<IPackage> Packages {
            get {
                return Visited.Keys;
            }
        }

        public void MarkProcessing(IPackage package) {
            Visited[package] = VisitedState.Processing;
        }

        public void MarkVisited(IPackage package) {
            Visited[package] = VisitedState.Completed;
        }

        public bool IsVisited(IPackage package) {
            VisitedState packageVisitedState;
            return Visited.TryGetValue(package, out packageVisitedState) && packageVisitedState == VisitedState.Completed;
        }

        public bool IsCycle(IPackage package) {
            VisitedState packageVisitedState;
            return Visited.TryGetValue(package, out packageVisitedState) && packageVisitedState == VisitedState.Processing;
        }

        public void Reset() {
            Visited.Clear();
        }

        internal enum VisitedState {
            Processing,
            Completed
        }
    }
}

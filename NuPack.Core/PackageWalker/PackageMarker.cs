namespace NuPack {
    using System.Collections.Generic;

    internal class PackageMarker {
        public PackageMarker()
            : this(PackageComparer.IdComparer) {
        }

        public PackageMarker(IEqualityComparer<Package> comparer) {
            Visited = new Dictionary<Package, VisitedState>(comparer);
        }

        private IDictionary<Package, VisitedState> Visited {
            get;
            set;
        }

        public IEnumerable<Package> Packages {
            get {
                return Visited.Keys;
            }
        }

        public void MarkProcessing(Package package) {
            Visited[package] = VisitedState.Processing;
        }

        public void MarkVisited(Package package) {
            Visited[package] = VisitedState.Completed;
        }

        public bool IsVisited(Package package) {
            VisitedState packageVisitedState;
            return Visited.TryGetValue(package, out packageVisitedState) && packageVisitedState == VisitedState.Completed;
        }

        public bool IsCycle(Package package) {
            VisitedState packageVisitedState;
            return Visited.TryGetValue(package, out packageVisitedState) && packageVisitedState == VisitedState.Processing;
        }

        internal enum VisitedState {
            Processing,
            Completed
        }
    }
}
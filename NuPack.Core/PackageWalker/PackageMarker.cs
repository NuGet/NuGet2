namespace NuGet {
    using System.Collections.Generic;

    public class PackageMarker {
        public PackageMarker()
            : this(PackageComparer.IdComparer) {
        }

        public PackageMarker(IEqualityComparer<IPackage> comparer) {
            Visited = new Dictionary<IPackage, VisitedState>(comparer);
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

        internal enum VisitedState {
            Processing,
            Completed
        }
    }
}

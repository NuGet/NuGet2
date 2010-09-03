using System.IO;

namespace NuPack {
    internal class PathSearchFilter {
        public PathSearchFilter(string searchDirectory, string searchPattern, SearchOption searchOption) {
            SearchDirectory = searchDirectory;
            SearchPattern = searchPattern;
            SearchOption = searchOption;
        }

        public string SearchDirectory { get; private set; }

        public SearchOption SearchOption { get; private set; }

        public string SearchPattern { get; private set; }
    }
}

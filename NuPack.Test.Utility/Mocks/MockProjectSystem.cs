namespace NuGet.Test.Mocks {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class MockProjectSystem : ProjectSystem {
        public MockProjectSystem() {
            References = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Paths = new Dictionary<string, Func<Stream>>(StringComparer.OrdinalIgnoreCase);
            Deleted = new HashSet<string>();
        }

        public Dictionary<string, string> References {
            get;
            private set;
        }

        public IDictionary<string, Func<Stream>> Paths {
            get;
            private set;
        }

        public HashSet<string> Deleted {
            get;
            private set;
        }

        public void CreateDirectory(string path) {
            Paths.Add(path, null);
        }

        public override void DeleteDirectory(string path, bool recursive = false) {
            foreach (var file in Paths.Keys.ToList()) {
                if (file.StartsWith(path)) {
                    Paths.Remove(file);
                }
            }
            Deleted.Add(path);
        }

        public override IEnumerable<string> GetFiles(string path) {
            return Paths.Select(f => f.Key)
                        .Where(f => Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public override IEnumerable<string> GetFiles(string path, string filter) {
            Regex matcher = GetFilterRegex(filter);

            return GetFiles(path).Where(f => matcher.IsMatch(f));
        }

        private static Regex GetFilterRegex(string wildcard) {
            string pattern = String.Join(String.Empty, wildcard.Split('.').Select(GetPattern));
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        private static string GetPattern(string token) {
            return token == "*" ? @"(.*)" : @"(" + token + ")";
        }

        public override void DeleteFile(string path) {
            Paths.Remove(path);
            Deleted.Add(path);
        }

        public override bool FileExists(string path) {
            return Paths.ContainsKey(path);
        }

        public override Stream OpenFile(string path) {
            return Paths[path]();
        }

        public override bool DirectoryExists(string path) {
            return Paths.Select(file => file.Key)
                        .Any(file => Path.GetDirectoryName(file).Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public override IEnumerable<string> GetDirectories(string path) {
            return Paths.GroupBy(f => Path.GetDirectoryName(f.Key))
                        .SelectMany(g => FileSystemExtensions.GetDirectories(g.Key))
                        .Where(f => !String.IsNullOrEmpty(f) && 
                               Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase))
                        .Distinct();
        }

        public void AddFile(string path) {
            AddFile(path, new MemoryStream());
        }

        public override void AddFile(string path, Stream stream) {
            var ms = new MemoryStream((int)stream.Length);
            stream.CopyTo(ms);
            byte[] buffer = ms.ToArray();
            Paths[path] = () => new MemoryStream(buffer);
        }

        public override void AddReference(string referencePath) {
            References.Add(Path.GetFileName(referencePath), referencePath);
        }

        public override void RemoveReference(string name) {
            References.Remove(name);
            DeleteFile(name);
        }

        public override string Root {
            get {
                return @"C:\MockFileSystem\";
            }
        }

        public override bool ReferenceExists(string name) {
            return References.ContainsKey(name);
        }

        public override DateTimeOffset GetLastModified(string path) {
            return DateTime.Now;
        }
    }
}

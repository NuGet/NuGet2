using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NuGet.Test.Mocks {
    public class MockFileSystem : IFileSystem {
        private ILogger _logger;

        public MockFileSystem() {
            Paths = new Dictionary<string, Func<Stream>>(StringComparer.OrdinalIgnoreCase);
            Deleted = new HashSet<string>();
        }

        public virtual ILogger Logger {
            get {
                return _logger ?? NullLogger.Instance;
            }
            set {
                _logger = value;
            }
        }

        public virtual string Root {
            get {
                return @"C:\MockFileSystem\";
            }
        }

        public virtual IDictionary<string, Func<Stream>> Paths {
            get;
            private set;
        }

        public virtual HashSet<string> Deleted {
            get;
            private set;
        }

        public virtual void CreateDirectory(string path) {
            Paths.Add(path, null);
        }

        public virtual void DeleteDirectory(string path, bool recursive = false) {
            foreach (var file in Paths.Keys.ToList()) {
                if (file.StartsWith(path)) {
                    Paths.Remove(file);
                }
            }
            Deleted.Add(path);
        }

        public virtual string GetFullPath(string path) {
            return Path.Combine(Root, path);
        }

        public virtual IEnumerable<string> GetFiles(string path) {
            return Paths.Select(f => f.Key)
                        .Where(f => Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter) {
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

        public virtual void DeleteFile(string path) {
            Paths.Remove(path);
            Deleted.Add(path);
        }

        public virtual bool FileExists(string path) {
            return Paths.ContainsKey(path);
        }

        public virtual Stream OpenFile(string path) {
            return Paths[path]();
        }

        public string ReadAllText(string path) {
            return OpenFile(path).ReadToEnd();
        }

        public virtual bool DirectoryExists(string path) {
            return Paths.Select(file => file.Key)
                        .Any(file => Path.GetDirectoryName(file).Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public virtual IEnumerable<string> GetDirectories(string path) {
            return Paths.GroupBy(f => Path.GetDirectoryName(f.Key))
                        .SelectMany(g => FileSystemExtensions.GetDirectories(g.Key))
                        .Where(f => !String.IsNullOrEmpty(f) &&
                               Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase))
                        .Distinct();
        }

        public virtual void AddFile(string path) {
            AddFile(path, new MemoryStream());
        }

        public void AddFile(string path, string content) {
            AddFile(path, content.AsStream());
        }

        public virtual void AddFile(string path, Stream stream) {
            var ms = new MemoryStream((int)stream.Length);
            stream.CopyTo(ms);
            byte[] buffer = ms.ToArray();
            Paths[path] = () => new MemoryStream(buffer);
        }

        public virtual void AddFile(string path, Func<Stream> getStream) {
            Paths[path] = getStream;
        }

        public virtual DateTimeOffset GetLastModified(string path) {
            return DateTime.UtcNow;
        }

        public virtual DateTimeOffset GetCreated(string path) {
            return DateTime.UtcNow;
        } 
    }
}

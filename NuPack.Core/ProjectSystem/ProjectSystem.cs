namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Versioning;

    public abstract class ProjectSystem : IFileSystem {
        private FrameworkName _targetFramework;
        private ILogger _logger;

        public ILogger Logger {
            get {
                return _logger ?? NullLogger.Instance;
            }
            set {
                _logger = value;
            }
        }

        public virtual FrameworkName TargetFramework {
            get {
                if (_targetFramework == null) {
                    _targetFramework = Utility.GetDefaultTargetFramework();
                }
                return _targetFramework;
            }
        }

        public virtual string ProjectName {
            get {
                return Root;
            }
        }

        public abstract string Root {
            get;
        }

        public abstract void AddFile(string path, Stream stream);
        public abstract void AddReference(string referencePath);
        public abstract void DeleteFile(string path);
        public abstract void DeleteDirectory(string path, bool recursive);
        public abstract bool DirectoryExists(string path);
        public abstract bool FileExists(string path);
        public abstract DateTimeOffset GetLastModified(string path);
        public abstract bool ReferenceExists(string name);

        public virtual dynamic GetPropertyValue(string propertyName) {
            return null;
        }

        public abstract IEnumerable<string> GetFiles(string path, string filter);
        public abstract IEnumerable<string> GetFiles(string path);
        public abstract IEnumerable<string> GetDirectories(string path);
        public abstract Stream OpenFile(string path);
        public abstract void RemoveReference(string name);        
    }
}

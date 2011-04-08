using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace NuGet.Test.Mocks {
    public class MockProjectSystem : MockFileSystem, IProjectSystem {
        private readonly FrameworkName _frameworkName;

        public MockProjectSystem()
            : this(VersionUtility.DefaultTargetFramework) {
        }

        public MockProjectSystem(FrameworkName frameworkName) {
            _frameworkName = frameworkName;
            References = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual Dictionary<string, string> References {
            get;
            private set;
        }

        public void AddReference(string referencePath) {
            AddReference(referencePath, null);
        }

        public virtual void AddReference(string referencePath, Stream stream) {
            References.Add(Path.GetFileName(referencePath), referencePath);
        }

        public virtual void RemoveReference(string name) {
            References.Remove(name);
            DeleteFile(name);
        }

        public virtual bool ReferenceExists(string name) {
            return References.ContainsKey(name);
        }

        public virtual FrameworkName TargetFramework {
            get { return _frameworkName; }
        }

        public virtual dynamic GetPropertyValue(string propertyName) {
            return null;
        }

        public virtual string ProjectName {
            get { return Root; }
        }

        public virtual bool IsSupportedFile(string path) {
            return true;
        }

        public void AddFrameworkReference(string name) {
            References[name] = name;
        }


        public virtual string ResolvePath(string path) {
            return path;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace NuGet.Test.Mocks {
    public class MockProjectSystem : MockFileSystem, IProjectSystem {        
        public MockProjectSystem() {
            References = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual Dictionary<string, string> References {
            get;
            private set;
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
            get { return VersionUtility.DefaultTargetFramework; }
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
    }
}

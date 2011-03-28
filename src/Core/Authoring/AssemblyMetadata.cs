using System;

namespace NuGet {
    [Serializable]
    public class AssemblyMetadata {
        public string Name { get; set; }
        public Version Version { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Company { get; set; }
    }
}

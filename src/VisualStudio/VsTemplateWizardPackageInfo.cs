using System;
using System.Diagnostics;

namespace NuGet.VisualStudio
{
    internal sealed class VsTemplateWizardPackageInfo
    {
        public VsTemplateWizardPackageInfo(string id, string version) :
            this(id, version, skipAssemblyReferences: false)
        {
        }

        public VsTemplateWizardPackageInfo(string id, string version, bool skipAssemblyReferences)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(id));
            Debug.Assert(!String.IsNullOrWhiteSpace(version));

            Id = id;
            Version = new SemanticVersion(version);
            SkipAssemblyReferences = skipAssemblyReferences;
        }

        public string Id { get; private set; }
        public SemanticVersion Version { get; private set; }
        public bool SkipAssemblyReferences { get; private set; }
    }
}
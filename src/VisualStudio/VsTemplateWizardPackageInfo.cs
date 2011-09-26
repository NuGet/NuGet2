using System;
using System.Diagnostics;

namespace NuGet.VisualStudio {
    internal sealed class VsTemplateWizardPackageInfo {
        public VsTemplateWizardPackageInfo(string id, string version) {
            Debug.Assert(!String.IsNullOrWhiteSpace(id));
            Debug.Assert(!String.IsNullOrWhiteSpace(version));

            Id = id;
            Version = new SemVer(version);
        }

        public string Id { get; private set; }
        public SemVer Version { get; private set; }
    }
}
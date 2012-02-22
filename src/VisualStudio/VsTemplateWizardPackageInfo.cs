using System;
using System.Diagnostics;

namespace NuGet.VisualStudio
{
    internal sealed class VsTemplateWizardPackageInfo
    {
        public VsTemplateWizardPackageInfo(string id, string version) :
            this(id, version, createRefreshFilesInBin: false)
        {
        }

        public VsTemplateWizardPackageInfo(string id, string version, bool createRefreshFilesInBin)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(id));
            Debug.Assert(!String.IsNullOrWhiteSpace(version));

            Id = id;
            Version = new SemanticVersion(version);
            CreateRefreshFilesInBin = createRefreshFilesInBin;
        }

        public string Id { get; private set; }
        public SemanticVersion Version { get; private set; }
        public bool CreateRefreshFilesInBin { get; private set; }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuGet.Client.Resolution;

namespace NuGet.Client.VisualStudio.UI
{
    public class PreviewWindowModel
    {
        private IEnumerable<PreviewResult> _previewResults;

        public IEnumerable<PreviewResult> PreviewResults
        {
            get
            {
                return _previewResults;
            }
        }

        public PreviewWindowModel(IEnumerable<PackageAction> actions, Installation.InstallationTarget target)
        {
            _previewResults = PreviewResult.CreatePreview(actions, target);
        }
    }
}
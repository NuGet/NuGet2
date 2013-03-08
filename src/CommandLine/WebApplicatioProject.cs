using System;
using System.IO;
using Microsoft.Build.BuildEngine;
using NuGet.Common;

#pragma warning disable 618

namespace NuGet
{
    class WebApplicatioProject : MSBuildProjectSystem
    {
        internal WebApplicatioProject(Project project, Language lang)
            : base(project, lang)
        {
        }

        public override bool IsSupportedFile(string path)
        {
            string fileName = Path.GetFileName(path);
            return !(fileName.StartsWith("app.", StringComparison.OrdinalIgnoreCase) &&
                     fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase));
        }
    }
}

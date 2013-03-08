using System.IO;
using Microsoft.Build.BuildEngine;

#pragma warning disable 618

namespace NuGet.Common
{
    class WixProjectSystem: MSBuildProjectSystem
    {
        internal WixProjectSystem(Project project)
            : base(project, Language.None)
        {
        }

        protected override string GetBuildAction(string relativePath)
        {
            switch (Path.GetExtension(relativePath).ToUpperInvariant())
            {
                case ".WXS":
                    return "Compile";
                default:
                    return "Content";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet
{
    public static class MSBuildProjectUtility
    {
        public static void AddEnsureImportedTarget(MsBuildProject buildProject, string targetsPath)
        {
            // first remove the target if it already exits.
            RemoveEnsureImportedTarget(buildProject, targetsPath);

            var targetName = GetNameOfEnsureImportedTarget(targetsPath);
            var target = buildProject.Xml.AddTarget(targetName);
            target.Condition = "!Exists('" + targetsPath + "')";

            // PrepareForBuild is used here because BeforeBuild does not work for VC++ projects.
            target.BeforeTargets = "PrepareForBuild";

            var errorTask = target.AddTask("Error");
            var errorText = string.Format(
                CultureInfo.InvariantCulture,
                CommonResources.EnsureImportedMessage,
                targetsPath);
            errorTask.SetParameter("Text", errorText);
        }

        public static void RemoveEnsureImportedTarget(MsBuildProject buildProject, string targetsPath)
        {
            if (buildProject.Xml.Targets != null)
            {
                var targetName = GetNameOfEnsureImportedTarget(targetsPath);
                var targetElement = buildProject.Xml.Targets.FirstOrDefault(
                    target => target.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
                if (targetElement != null)
                {
                    targetElement.Parent.RemoveChild(targetElement);
                }
            }
        }

        /// <summary>
        /// Returns the name of the target that is used to ensure targetsPath is imported.
        /// </summary>
        /// <param name="targetsPath">The targetsPath.</param>
        /// <returns>the name of the target.</returns>
        private static string GetNameOfEnsureImportedTarget(string targetsPath)
        {
            var pathBytes = Encoding.UTF8.GetBytes(targetsPath);
            var hashProvider = new CryptoHashProvider("SHA256");
            var hash = new SoapHexBinary(hashProvider.CalculateHash(pathBytes)).ToString();
            return "EnsureImported" + hash;
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Build.Construction;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet
{
    public static class MSBuildProjectUtility
    {
        private const string targetName = "EnsureNuGetPackageBuildImports";

        public static void AddEnsureImportedTarget(MsBuildProject buildProject, string targetsPath)
        {
            // get the target
            var targetElement = buildProject.Xml.Targets.FirstOrDefault(
                target => target.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

            // if the target does not exist, create the target
            if (targetElement == null)
            {
                targetElement = buildProject.Xml.AddTarget(targetName);

                // PrepareForBuild is used here because BeforeBuild does not work for VC++ projects.
                targetElement.BeforeTargets = "PrepareForBuild";

                var propertyGroup = targetElement.AddPropertyGroup();
                propertyGroup.AddProperty("ErrorText", CommonResources.EnsureImportedMessage);
            }

            var errorTask = targetElement.AddTask("Error");
            errorTask.Condition = "!Exists('" + targetsPath + "')";
            var errorText = string.Format(
                CultureInfo.InvariantCulture,
                @"$([System.String]::Format('$(ErrorText)', '{0}'))",
                targetsPath);
            errorTask.SetParameter("Text", errorText);
        }

        public static void RemoveEnsureImportedTarget(MsBuildProject buildProject, string targetsPath)
        {
            var targetElement = buildProject.Xml.Targets.FirstOrDefault(
                target => string.Equals(target.Name, targetName, StringComparison.OrdinalIgnoreCase));
            if (targetElement == null)
            {
                return;
            }

            string errorCondition = "!Exists('" + targetsPath + "')";
            var taskElement = targetElement.Tasks.FirstOrDefault(
                task => string.Equals(task.Condition, errorCondition, StringComparison.OrdinalIgnoreCase));
            if (taskElement == null)
            {
                return;
            }

            taskElement.Parent.RemoveChild(taskElement);
            if (targetElement.Tasks.Count == 0)
            {
                targetElement.Parent.RemoveChild(targetElement);
            }
        }
    }
}

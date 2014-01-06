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

        /// <summary>
        /// Adds an Import element to this project file if it doesn't already exist.            
        /// </summary>
        /// <param name="project">The project file.</param>
        /// <param name="targetsPath">The path to the imported file.</param>
        /// <param name="location">The location where the Import is added.</param>
        public static void AddImportStatement(MsBuildProject project, string targetsPath, ProjectImportLocation location)
        {
            if (project.Xml.Imports == null ||
                project.Xml.Imports.All(import => !targetsPath.Equals(import.Project, StringComparison.OrdinalIgnoreCase)))
            {
                ProjectImportElement pie = project.Xml.AddImport(targetsPath);
                pie.Condition = "Exists('" + targetsPath + "')";
                if (location == ProjectImportLocation.Top)
                {
                    // There's no public constructor to create a ProjectImportElement directly.
                    // So we have to cheat by adding Import at the end, then remove it and insert at the beginning
                    pie.Parent.RemoveChild(pie);
                    project.Xml.InsertBeforeChild(pie, project.Xml.FirstChild);
                }

                NuGet.MSBuildProjectUtility.AddEnsureImportedTarget(project, targetsPath);
                project.ReevaluateIfNecessary();
            }
        }

        /// <summary>
        /// Removes the Import element from the project file.
        /// </summary>
        /// <param name="project">The project file.</param>
        /// <param name="targetsPath">The path to the imported file.</param>
        public static void RemoveImportStatement(MsBuildProject project, string targetsPath)
        {
            if (project.Xml.Imports != null)
            {
                // search for this import statement and remove it
                var importElement = project.Xml.Imports.FirstOrDefault(
                    import => targetsPath.Equals(import.Project, StringComparison.OrdinalIgnoreCase));

                if (importElement != null)
                {
                    importElement.Parent.RemoveChild(importElement);
                    NuGet.MSBuildProjectUtility.RemoveEnsureImportedTarget(project, targetsPath);
                    project.ReevaluateIfNecessary();
                }
            }
        }

        private static void AddEnsureImportedTarget(MsBuildProject buildProject, string targetsPath)
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

        private static void RemoveEnsureImportedTarget(MsBuildProject buildProject, string targetsPath)
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

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Build.Evaluation;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio
{
    internal static class MSBuildProjectExtensions
    {
        private const string ReferenceProjectItem = "Reference";

        internal static IEnumerable<Tuple<ProjectItem, AssemblyName>> GetAssemblyReferences(this MsBuildProject project)
        {
            foreach (ProjectItem referenceProjectItem in project.GetItems(ReferenceProjectItem))
            {
                AssemblyName assemblyName = null;
                try
                {
                    assemblyName = new AssemblyName(referenceProjectItem.EvaluatedInclude);
                }
                catch (Exception exception)
                {
                    ExceptionHelper.WriteToActivityLog(exception);
                    // Swallow any exceptions we might get because of malformed assembly names
                }

                // We can't yield from within the try so we do it out here if everything was successful
                if (assemblyName != null)
                {
                    yield return Tuple.Create(referenceProjectItem, assemblyName);
                }
            }
        }
    }
}

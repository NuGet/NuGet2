using Microsoft.VisualStudio.VCProjectEngine;

namespace NuGet.VisualStudio10
{
    public static class VCProjectHelper
    {
        public static void AddProjectFilter(object project, string folderPath)
        {
            var vcProject = project as VCProject;
            if (vcProject != null)
            {
                vcProject.AddFilter(folderPath);
            }
        }
    }
}
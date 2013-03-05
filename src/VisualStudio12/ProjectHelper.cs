using System;
using Microsoft.VisualStudio.Shell.Interop;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio12
{
    public static class ProjectHelper
    {
        public static void DoWorkInWriterLock(IVsHierarchy hierarchy, Action<MsBuildProject> action)
        {
            // Because this project is only used as a reference assembly to make the compiler happy,
            // we don't need to do anything here. 
            // During the CI build, we replace this assembly with the real one which contains actual implementation. 
        }
    }
}
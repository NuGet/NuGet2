using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.Shell.Interop;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio12
{
    public static class ProjectHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async void DoWorkInWriterLock(IVsHierarchy hierarchy, Action<MsBuildProject> action)
        {
            IVsBrowseObjectContext context = hierarchy as IVsBrowseObjectContext;
            if (context != null)
            {
                var service = context.UnconfiguredProject.ProjectService.Services.ProjectLockService;
                if (service != null)
                {
                    using (ProjectWriteLockReleaser x = await service.WriteLockAsync())
                    {
                        await x.CheckoutAsync(context.UnconfiguredProject.FullPath);
                        MsBuildProject buildProject = await x.GetProjectAsync(context.UnconfiguredProject.Services.SuggestedConfiguredProject);

                        action(buildProject);

                        await x.ReleaseAsync();
                    }

                    await context.UnconfiguredProject.ProjectService.Services.ThreadingPolicy.SwitchToUIThread();
                }
            }
        }
    }
}
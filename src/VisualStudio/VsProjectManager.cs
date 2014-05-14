using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    public class VsProjectManager : ProjectManager
    {
        VsPackageManager _packageManager;

        public VsProjectManager(
            VsPackageManager packageManager, 
            IPackagePathResolver pathResolver, 
            IProjectSystem project, 
            IPackageRepository localRepository)
            : base(packageManager, pathResolver, project, localRepository)
        {
            _packageManager = packageManager;
        }

        public override void Execute(PackageOperation op)
        {
            // Try to get the project for this project manager
            Project project = _packageManager.GetProject(this);
            IVsProjectBuildSystem build = null;
            if (project != null)
            {
                build = project.ToVsProjectBuildSystem();
            }

            try
            {
                if (build != null)
                {
                    // Start a batch edit so there is no background compilation until we're done
                    // processing project actions
                    build.StartBatchEdit();
                }

                base.Execute(op);
            }
            finally
            {
                if (build != null)
                {
                    // End the batch edit when we are done.
                    build.EndBatchEdit();
                }
            }

            var eventArgs = CreateOperation(op.Package);
            if (op.Action == PackageAction.Install)
            {
                _packageManager.PackageEvents.NotifyReferenceAdded(eventArgs);
            }
            else
            {
                _packageManager.PackageEvents.NotifyReferenceRemoved(eventArgs);
            }
        }

        public override string ToString()
        {
            return Project.ProjectName;
        }
    }
}

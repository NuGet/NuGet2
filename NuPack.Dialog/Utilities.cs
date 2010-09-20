using System;

namespace NuPack.Dialog {
    public class Utilities {
        public static IServiceProvider ServiceProvider {
            get;
            set;
        }

        public static T GetService<S, T>() where T : class {
            if (ServiceProvider == null) { return null; }
            else { return ServiceProvider.GetService(typeof(S)) as T; }
        }

        public static EnvDTE.Project GetActiveProject(EnvDTE._DTE dte) {
            EnvDTE.Project activeProject = null;

            if (dte != null) {
                Object obj = dte.ActiveSolutionProjects;
                if (obj != null && obj is Array && ((Array)obj).Length > 0) {
                    Object proj = ((Array)obj).GetValue(0);

                    if (proj != null && proj is EnvDTE.Project) {
                        activeProject = (EnvDTE.Project)proj;
                    }
                }
            }
            return activeProject;
        }
    }
}

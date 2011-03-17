using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using VsPackage = Microsoft.VisualStudio.Shell.Package;

namespace NuGetConsole {
    [Export(typeof(IConsoleInitializer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ConsoleInitializer : IConsoleInitializer {
        private readonly Lazy<Task<Action>> _initializeTask = new Lazy<Task<Action>>(GetInitializeTask);

        public Task<Action> Initialize() {
            return _initializeTask.Value;
        }

        private static Task<Action> GetInitializeTask() {
            var componentModel = (IComponentModel)VsPackage.GetGlobalService(typeof(SComponentModel));
            if (componentModel == null) {
                throw new InvalidOperationException();
            }

            var initializer = componentModel.GetService<IHostInitializer>();
            return Task.Factory.StartNew(() => {
                initializer.Start();
                return new Action(initializer.SetDefaultRunspace);
            });
        }
    }

}

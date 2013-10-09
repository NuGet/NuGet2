using System;
using System.ComponentModel.Composition;
using Microsoft.WebMatrix.Core;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Shared state and services of the NuGet package
    /// </summary>
    internal class ModuleGlobals
    {
        private CommandManager _commandManager;

        public ModuleGlobals(IWebMatrixHost host, IServiceProvider serviceProvider)
        {
            this.Host = host;
            this.ServiceProvider = serviceProvider;

            var compositionService = this.ServiceProvider.GetService<CompositionService>();
            compositionService.SatisfyImportsOnce(this);

            this.CommandTarget = new NuGetCommandTarget(this);
        }

        public CommandManager CommandManager
        {
            get
            {
                if (_commandManager == null)
                {
                    _commandManager = this.ServiceProvider.GetService<CommandManager>();
                }

                return _commandManager;
            }
        }

        public NuGetCommandTarget CommandTarget
        {
            get;
            private set;
        }

        public IWebMatrixHost Host
        {
            get;
            private set;
        }

        [Import]
        public INuGetExtensionGallery Gallery
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get;
            set;
        }
    }
}

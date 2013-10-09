using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;

namespace NuGet.WebMatrix
{
    [Export(typeof(Extension))]
    [Export(typeof(NuGetExtension))]
    internal class NuGetExtension : Extension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:NuGetExtension"/> class.
        /// </summary>
        public NuGetExtension()
            : base(Resources.ExtensionName)
        {
        }

        public ModuleGlobals Globals
        {
            get;
            private set;
        }

        public IWebMatrixHostInternal Host
        {
            get;
            private set;
        }

        internal static TaskScheduler GetCurrentTaskScheduler()
        {
            TaskScheduler scheduler = null;
            try
            {
                // the scheduler should be the current Sync Context
                scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException)
            {
                scheduler = TaskScheduler.Default;
            }

            return scheduler;
        }

        protected override void  Initialize(IWebMatrixHost host, ExtensionInitData initData)
        {
            this.Host = (IWebMatrixHostInternal)host;
            this.Globals = new ModuleGlobals(this.Host, this.Host.ServiceProvider);

            this.Host.WebSiteChanged += this.Host_WebSiteChanged;
        }

        private void Host_WebSiteChanged(object sender, EventArgs e)
        {
            NuGetModel.ClearCache();
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.WebMatrix.Extensibility;

namespace NuGet.WebMatrix
{
    [Export(typeof(IPackageManagerProvider))]
    internal class NuGetRibbonProvider : IPackageManagerProvider
    {
        private RibbonButton _nuGetButton;

        [Import]
        public NuGetExtension Extension
        {
            get;
            private set;
        }

        public RibbonButton RibbonButton
        {
            get
            {
                return this.NuGetButton;
            }
        }

        public bool ShouldShow(IEnumerable<SiteTechnologyType> siteTechnologies)
        {
            return siteTechnologies.Any(t => t == SiteTechnologyType.AspNet);
        }

        private RibbonButton NuGetButton
        {
            get
            {
                if (_nuGetButton == null)
                {
                    var commandManager = this.Extension.Globals.CommandManager;
                    var nuGetCommand = commandManager[NuGetCommands.OpenNuGetGalleryCommandId];
                    Debug.Assert(nuGetCommand != null, "NuGet command should not be null");

                    _nuGetButton = new RibbonButton(nuGetCommand.Label, nuGetCommand, null, nuGetCommand.SmallImage, nuGetCommand.LargeImage);
                }

                return _nuGetButton;
            }
        }
    }
}

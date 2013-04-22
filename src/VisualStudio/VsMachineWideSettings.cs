using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IMachineWideSettings))]
    public class VsMachineWideSettings : IMachineWideSettings
    {
        Lazy<IEnumerable<Settings>> _settings;

        [ImportingConstructor]
        public VsMachineWideSettings() : this(ServiceLocator.GetInstance<DTE>())
        {
        }

        internal VsMachineWideSettings(DTE dte)
        {
            var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _settings = new Lazy<IEnumerable<NuGet.Settings>>(
                () =>
                {
                    
                    return NuGet.Settings.LoadMachineWideSettings(
                        new PhysicalFileSystem(baseDirectory),
                        "VisualStudio",
                        dte.Version,
                        VsVersionHelper.GetSKU());
                });
        }

        public IEnumerable<Settings> Settings
        {
            get
            {
                return _settings.Value;
            }
        }
    }
}

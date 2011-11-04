using System;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IFileOpener))]
    public class VsFileOpener : IFileOpener
    {
        private readonly DTE _dte;

        public VsFileOpener() : this(ServiceLocator.GetInstance<DTE>())
        {
        }

        public VsFileOpener(DTE dte)
        {
            _dte = dte;
        }

        public bool OpenFile(string filePath)
        {
            if (filePath == null) 
            {
                throw new ArgumentNullException("filePath");
            }

            if (_dte.ItemOperations != null && File.Exists(filePath))
            {
                Window window = _dte.ItemOperations.OpenFile(filePath);
                return window != null;
            }

            return false;
        }
    }
}
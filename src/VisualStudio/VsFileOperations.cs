using System;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IFileOperations))]
    public class VsFileOperations : IFileOperations
    {
        private readonly DTE _dte;

        public VsFileOperations() : this(ServiceLocator.GetInstance<DTE>())
        {
        }

        public VsFileOperations(DTE dte)
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
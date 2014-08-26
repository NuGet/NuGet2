using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.VisualStudio.ClientV3;

namespace NuGet.Tools
{
    public class PackageManagerDocData : IVsPersistDocData
    {
        private readonly Guid _guidEditorType = new Guid(GuidList.guidEditorFactoryString);

        public PackageManagerSession Session { get; private set; }
        
        public PackageManagerDocData(PackageManagerSession context)
        {
            Session = context;
        }

        #region IVsPersistDocData

        public int Close()
        {
            return VSConstants.S_OK;
        }

        public int GetGuidEditorType(out Guid pClassID)
        {
            pClassID = _guidEditorType;
            return VSConstants.S_OK;
        }

        public int IsDocDataDirty(out int pfDirty)
        {
            pfDirty = 0;
            return VSConstants.S_OK;
        }

        public int IsDocDataReloadable(out int pfReloadable)
        {
            // Reload doesn't make sense
            pfReloadable = 0;
            return VSConstants.S_OK;
        }

        public int LoadDocData(string pszMkDocument)
        {
            return VSConstants.S_OK;
        }

        public int OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
        {
            return VSConstants.S_OK;
        }

        public int ReloadDocData(uint grfFlags)
        {
            return VSConstants.S_OK;
        }

        public int RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int SaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int pfSaveCanceled)
        {
            // We don't support save as so we don't need to the two out parameters.
            pbstrMkDocumentNew = null;
            pfSaveCanceled = 0;

            return VSConstants.S_OK;
        }

        public int SetUntitledDocPath(string pszDocDataPath)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion
    }
}

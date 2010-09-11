using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuPack.Dialog.PackageProject {
    class ProjectBase : ProjectBaseInterfaces {
        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy AdviseHierarchyEvents
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int AdviseHierarchyEvents(Microsoft.VisualStudio.Shell.Interop.IVsHierarchyEvents eventSink, out uint cookie) {
            cookie = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Close
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int Close() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetCanonicalName
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetCanonicalName(uint itemId, out string name) {
            name = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetGuidProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetGuidProperty(uint itemId, int propId, out Guid property) {
            property = Guid.Empty;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetNestedHierarchy
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetNestedHierarchy(uint itemId, ref System.Guid guidHierarchyNested, out System.IntPtr hierarchyNested, out uint itemIdNested) {
            hierarchyNested = IntPtr.Zero;
            itemIdNested = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetProperty(uint itemId, int propId, out Object property) {
            property = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetSite
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider) {
            serviceProvider = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy ParseCanonicalName
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int ParseCanonicalName(string name, out uint itemId) {
            itemId = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy QueryClose
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int QueryClose(out int canClose) {
            canClose = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy SetGuidProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int SetGuidProperty(uint itemId, int propId, ref System.Guid guid) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy UnadviseHierarchyEvents
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int UnadviseHierarchyEvents(uint cookie) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy SetProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int SetProperty(uint itemId, int propId, System.Object property) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy SetSite
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int SetSite(IOleServiceProvider serviceProvider) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsUIHierarchy QueryStatusCommand
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsUIHierarchy ExecCommand
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject IsDocumentInProject
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid) {
            int hr = VSConstants.E_NOTIMPL;
            pfFound = 0;
            pitemid = VSConstants.VSITEMID_NIL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject GetMkDocument
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetMkDocument(uint itemid, out string pbstrMkDocument) {
            pbstrMkDocument = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject OpenItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            ppWindowFrame = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject GetItemContext
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetItemContext(uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP) {
            ppSP = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject GenerateUniqueItemName 
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName) {
            pbstrItemName = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject AddItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject2 RemoveItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int RemoveItem(uint dwReserved, uint itemid, out int pfResult) {
            pfResult = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject2 ReopenItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            ppWindowFrame = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject3 AddItemWithSpecific
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int AddItemWithSpecific(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, VSADDRESULT[] pResult) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject3 OpenItemWithSpecific
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int OpenItemWithSpecific(uint itemid, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            ppWindowFrame = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject3 TransferItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int TransferItem(string pszMkDocumentOld, string pszMkDocumentNew, IVsWindowFrame punkWindowFrame) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 ContainsFileEndingWith
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int ContainsFileEndingWith(string pszEndingWith, out int pfDoesContain) {
            pfDoesContain = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 ContainsFileWithItemType
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int ContainsFileWithItemType(string pszItemType, out int pfDoesContain) {
            pfDoesContain = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 GetFilesEndingWith
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetFilesEndingWith(string pszEndingWith, uint celt, uint[] rgItemids, out uint pcActual) {
            pcActual = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 GetFilesWithItemType
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int GetFilesWithItemType(string pszItemType, uint celt, uint[] rgItemids, out uint pcActual) {
            pcActual = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IOleCommandTarget Exec
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IOleCommandTarget QueryStatus
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }
    }

    internal abstract class ProjectBaseInterfaces :
        IVsHierarchy,
        IVsUIHierarchy,
        IVsProject,
        IVsProject2,
        IVsProject3,
        IVsProject4,
        IOleCommandTarget {
        #region IVsHierarchy, IVsUIHierarchy

        /// <summary>
        /// IVsHierarchy, IVsUIHierarchy stubs
        /// </summary>
        int IVsHierarchy.AdviseHierarchyEvents(Microsoft.VisualStudio.Shell.Interop.IVsHierarchyEvents eventSink, out uint cookie) {
            return AdviseHierarchyEvents(eventSink, out cookie);
        }
        int IVsHierarchy.Close() {
            return Close();
        }
        int IVsHierarchy.GetCanonicalName(uint itemId, out string name) {
            return GetCanonicalName(itemId, out name);
        }
        int IVsHierarchy.GetGuidProperty(uint itemId, int propId, out System.Guid guid) {
            return GetGuidProperty(itemId, propId, out guid);
        }
        int IVsHierarchy.GetNestedHierarchy(uint itemId, ref System.Guid guidHierarchyNested, out System.IntPtr hierarchyNested, out uint itemIdNested) {
            return GetNestedHierarchy(itemId, ref guidHierarchyNested, out hierarchyNested, out itemIdNested);
        }
        int IVsHierarchy.GetProperty(uint itemId, int propId, out System.Object property) {
            return GetProperty(itemId, propId, out property);
        }
        int IVsHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider) {
            return GetSite(out serviceProvider);
        }
        int IVsHierarchy.ParseCanonicalName(string name, out uint itemId) {
            return ParseCanonicalName(name, out itemId);
        }
        int IVsHierarchy.QueryClose(out int canClose) {
            return QueryClose(out canClose);
        }
        int IVsHierarchy.SetGuidProperty(uint itemId, int propId, ref System.Guid guid) {
            return SetGuidProperty(itemId, propId, ref guid);
        }
        int IVsHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider) {
            return SetSite(serviceProvider);
        }
        int IVsHierarchy.UnadviseHierarchyEvents(uint cookie) {
            return UnadviseHierarchyEvents(cookie);
        }
        int IVsHierarchy.SetProperty(uint itemId, int propId, System.Object property) {
            return SetProperty(itemId, propId, property);
        }
        int IVsHierarchy.Unused0() {
            return Unused0();
        }
        int IVsHierarchy.Unused1() {
            return Unused1();
        }
        int IVsHierarchy.Unused2() {
            return Unused2();
        }
        int IVsHierarchy.Unused3() {
            return Unused3();
        }
        int IVsHierarchy.Unused4() {
            return Unused4();
        }
        int IVsUIHierarchy.QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            return QueryStatusCommand(itemid, ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
        int IVsUIHierarchy.ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            return ExecCommand(itemid, ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
        int IVsUIHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie) {
            return AdviseHierarchyEvents(pEventSink, out pdwCookie);
        }
        int IVsUIHierarchy.Close() {
            return Close();
        }
        int IVsUIHierarchy.GetCanonicalName(uint itemid, out string pbstrName) {
            return GetCanonicalName(itemid, out pbstrName);
        }
        int IVsUIHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid) {
            return GetGuidProperty(itemid, propid, out pguid);
        }
        int IVsUIHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested) {
            return GetNestedHierarchy(itemid, ref iidHierarchyNested, out ppHierarchyNested, out pitemidNested);
        }
        int IVsUIHierarchy.GetProperty(uint itemid, int propid, out object pvar) {
            return GetProperty(itemid, propid, out pvar);
        }
        int IVsUIHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP) {
            return GetSite(out ppSP);
        }
        int IVsUIHierarchy.ParseCanonicalName(string pszName, out uint pitemid) {
            return ParseCanonicalName(pszName, out pitemid);
        }
        int IVsUIHierarchy.QueryClose(out int pfCanClose) {
            return QueryClose(out pfCanClose);
        }
        int IVsUIHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid) {
            return SetGuidProperty(itemid, propid, ref rguid);
        }
        int IVsUIHierarchy.SetProperty(uint itemid, int propid, object var) {
            return SetProperty(itemid, propid, var);
        }
        int IVsUIHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp) {
            return SetSite(psp);
        }
        int IVsUIHierarchy.UnadviseHierarchyEvents(uint dwCookie) {
            return UnadviseHierarchyEvents(dwCookie);
        }
        int IVsUIHierarchy.Unused0() {
            return Unused0();
        }
        int IVsUIHierarchy.Unused1() {
            return Unused1();
        }
        int IVsUIHierarchy.Unused2() {
            return Unused2();
        }
        int IVsUIHierarchy.Unused3() {
            return Unused3();
        }
        int IVsUIHierarchy.Unused4() {
            return Unused4();
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy AdviseHierarchyEvents
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int AdviseHierarchyEvents(Microsoft.VisualStudio.Shell.Interop.IVsHierarchyEvents eventSink, out uint cookie) {
            cookie = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Close
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Close() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetCanonicalName
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetCanonicalName(uint itemId, out string name) {
            name = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetGuidProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetGuidProperty(uint itemId, int propId, out Guid property) {
            property = Guid.Empty;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetNestedHierarchy
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetNestedHierarchy(uint itemId, ref System.Guid guidHierarchyNested, out System.IntPtr hierarchyNested, out uint itemIdNested) {
            hierarchyNested = IntPtr.Zero;
            itemIdNested = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetProperty(uint itemId, int propId, out Object property) {
            property = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy GetSite
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider) {
            serviceProvider = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy ParseCanonicalName
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int ParseCanonicalName(string name, out uint itemId) {
            itemId = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy QueryClose
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int QueryClose(out int canClose) {
            canClose = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy SetGuidProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int SetGuidProperty(uint itemId, int propId, ref System.Guid guid) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy UnadviseHierarchyEvents
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int UnadviseHierarchyEvents(uint cookie) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy SetProperty
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int SetProperty(uint itemId, int propId, System.Object property) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy SetSite
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int SetSite(IOleServiceProvider serviceProvider) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Unused0
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Unused0() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Unused1
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Unused1() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Unused2
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Unused2() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Unused3
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Unused3() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsHierarchy Unused4
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Unused4() {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsUIHierarchy QueryStatusCommand
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsUIHierarchy ExecCommand
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        #endregion

        #region IVsProject, IVsProject2, IVsProject3, IVsProject4

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject, IVsProject2, IVsProject3 stubs
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        int IVsProject.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult) {
            return AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
        }
        int IVsProject.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName) {
            return GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
        }
        int IVsProject.GetItemContext(uint itemid, out IOleServiceProvider ppSP) {
            return GetItemContext(itemid, out ppSP);
        }
        int IVsProject.GetMkDocument(uint itemid, out string pbstrMkDocument) {
            return GetMkDocument(itemid, out pbstrMkDocument);
        }
        int IVsProject.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid) {
            return IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
        }
        int IVsProject.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject2.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult) {
            return AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
        }
        int IVsProject2.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName) {
            return GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
        }
        int IVsProject2.GetItemContext(uint itemid, out IOleServiceProvider ppSP) {
            return GetItemContext(itemid, out ppSP);
        }
        int IVsProject2.GetMkDocument(uint itemid, out string pbstrMkDocument) {
            return GetMkDocument(itemid, out pbstrMkDocument);
        }
        int IVsProject2.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid) {
            return IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
        }
        int IVsProject2.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject2.RemoveItem(uint dwReserved, uint itemid, out int pfResult) {
            return RemoveItem(dwReserved, itemid, out pfResult);
        }
        int IVsProject2.ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return ReopenItem(itemid, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject3.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult) {
            return AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
        }
        int IVsProject3.AddItemWithSpecific(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, VSADDRESULT[] pResult) {
            return AddItemWithSpecific(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, grfEditorFlags, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, pResult);
        }
        int IVsProject3.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName) {
            return GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
        }
        int IVsProject3.GetItemContext(uint itemid, out IOleServiceProvider ppSP) {
            return GetItemContext(itemid, out ppSP);
        }
        int IVsProject3.GetMkDocument(uint itemid, out string pbstrMkDocument) {
            return GetMkDocument(itemid, out pbstrMkDocument);
        }
        int IVsProject3.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid) {
            return IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
        }
        int IVsProject3.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject3.OpenItemWithSpecific(uint itemid, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return OpenItemWithSpecific(itemid, grfEditorFlags, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject3.RemoveItem(uint dwReserved, uint itemid, out int pfResult) {
            return RemoveItem(dwReserved, itemid, out pfResult);
        }
        int IVsProject3.ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return ReopenItem(itemid, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject3.TransferItem(string pszMkDocumentOld, string pszMkDocumentNew, IVsWindowFrame punkWindowFrame) {
            return TransferItem(pszMkDocumentOld, pszMkDocumentNew, punkWindowFrame);
        }
        int IVsProject4.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult) {
            return AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
        }
        int IVsProject4.AddItemWithSpecific(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, VSADDRESULT[] pResult) {
            return AddItemWithSpecific(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, grfEditorFlags, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, pResult);
        }
        int IVsProject4.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName) {
            return GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
        }
        int IVsProject4.GetItemContext(uint itemid, out IOleServiceProvider ppSP) {
            return GetItemContext(itemid, out ppSP);
        }
        int IVsProject4.GetMkDocument(uint itemid, out string pbstrMkDocument) {
            return GetMkDocument(itemid, out pbstrMkDocument);
        }
        int IVsProject4.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid) {
            return IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
        }
        int IVsProject4.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject4.OpenItemWithSpecific(uint itemid, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return OpenItemWithSpecific(itemid, grfEditorFlags, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject4.RemoveItem(uint dwReserved, uint itemid, out int pfResult) {
            return RemoveItem(dwReserved, itemid, out pfResult);
        }
        int IVsProject4.ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            return ReopenItem(itemid, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
        }
        int IVsProject4.TransferItem(string pszMkDocumentOld, string pszMkDocumentNew, IVsWindowFrame punkWindowFrame) {
            return TransferItem(pszMkDocumentOld, pszMkDocumentNew, punkWindowFrame);
        }
        int IVsProject4.ContainsFileEndingWith(string pszEndingWith, out int pfDoesContain) {
            return ContainsFileEndingWith(pszEndingWith, out pfDoesContain);
        }
        int IVsProject4.ContainsFileWithItemType(string pszItemType, out int pfDoesContain) {
            return ContainsFileWithItemType(pszItemType, out pfDoesContain);
        }
        int IVsProject4.GetFilesEndingWith(string pszEndingWith, uint celt, uint[] rgItemids, out uint pcActual) {
            return GetFilesEndingWith(pszEndingWith, celt, rgItemids, out pcActual);
        }
        int IVsProject4.GetFilesWithItemType(string pszItemType, uint celt, uint[] rgItemids, out uint pcActual) {
            return GetFilesWithItemType(pszItemType, celt, rgItemids, out pcActual);
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject IsDocumentInProject
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid) {
            int hr = VSConstants.E_NOTIMPL;
            pfFound = 0;
            pitemid = VSConstants.VSITEMID_NIL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject GetMkDocument
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetMkDocument(uint itemid, out string pbstrMkDocument) {
            pbstrMkDocument = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject OpenItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            ppWindowFrame = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject GetItemContext
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetItemContext(uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP) {
            ppSP = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject GenerateUniqueItemName 
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName) {
            pbstrItemName = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject AddItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject2 RemoveItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int RemoveItem(uint dwReserved, uint itemid, out int pfResult) {
            pfResult = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject2 ReopenItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            ppWindowFrame = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject3 AddItemWithSpecific
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int AddItemWithSpecific(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, VSADDRESULT[] pResult) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject3 OpenItemWithSpecific
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int OpenItemWithSpecific(uint itemid, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame) {
            ppWindowFrame = null;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject3 TransferItem
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int TransferItem(string pszMkDocumentOld, string pszMkDocumentNew, IVsWindowFrame punkWindowFrame) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 ContainsFileEndingWith
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int ContainsFileEndingWith(string pszEndingWith, out int pfDoesContain) {
            pfDoesContain = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 ContainsFileWithItemType
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int ContainsFileWithItemType(string pszItemType, out int pfDoesContain) {
            pfDoesContain = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 GetFilesEndingWith
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetFilesEndingWith(string pszEndingWith, uint celt, uint[] rgItemids, out uint pcActual) {
            pcActual = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IVsProject4 GetFilesWithItemType
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int GetFilesWithItemType(string pszItemType, uint celt, uint[] rgItemids, out uint pcActual) {
            pcActual = 0;
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        #endregion

        #region IOleCommandTarget

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IOleCommandTarget stubs
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            return Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            return QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IOleCommandTarget Exec
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        ///--------------------------------------------------------------------------------------------
        /// <summary>
        /// IOleCommandTarget QueryStatus
        /// </summary>
        ///--------------------------------------------------------------------------------------------
        protected virtual int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            int hr = VSConstants.E_NOTIMPL;
            return hr;
        }

        #endregion
    }
}

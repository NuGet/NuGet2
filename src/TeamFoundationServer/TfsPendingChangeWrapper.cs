using System;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer
{
    public class TfsPendingChangeWrapper : ITfsPendingChange
    {
        private readonly PendingChange _pendingChange;

        public TfsPendingChangeWrapper(PendingChange pendingChange)
        {
            if (pendingChange == null)
            {
                throw new ArgumentNullException("pendingChange");
            }

            _pendingChange = pendingChange;
        }

        public bool IsAdd
        {
            get
            {
                return _pendingChange.IsAdd;
            }
        }

        public bool IsDelete
        {
            get
            {
                return _pendingChange.IsDelete;
            }
        }

        public bool IsEdit
        {
            get
            {
                return _pendingChange.IsEdit;
            }
        }

        public string LocalItem
        {
            get
            {
                return _pendingChange.LocalItem;
            }
        }

        internal PendingChange PendingChange
        {
            get
            {
                return _pendingChange;
            }
        }
    }
}
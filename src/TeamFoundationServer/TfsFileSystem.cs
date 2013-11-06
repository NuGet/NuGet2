using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using NuGet.Resources;
using NuGet.VisualStudio;

namespace NuGet.TeamFoundationServer
{
    public class TfsFileSystem : PhysicalFileSystem, ISourceControlFileSystem, IBatchProcessor<string>
    {
        // A flag indicating whether TFS operation should be performed or not in single file operation.
        // This flag is set to false by DeleteFiles() and AddFiles() so that TFS operation is 
        // skipped during single file processing since they will add / remove files from TFS in
        // bulk mode.
        bool _disableTfs;

        public TfsFileSystem(Workspace workspace, string path)
            : this(new TfsWorkspaceWrapper(workspace), path)
        {
        }

        public TfsFileSystem(ITfsWorkspace workspace, string path)
            : base(path)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            Workspace = workspace;
            _disableTfs = false;
        }

        public ITfsWorkspace Workspace { get; private set; }

        public override void AddFile(string path, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            AddFileCore(path, () => base.AddFile(path, stream));
        }

        public override void AddFile(string path, Action<Stream> writeToStream)
        {
            if (writeToStream == null)
            {
                throw new ArgumentNullException("writeToStream");
            }

            AddFileCore(path, () => base.AddFile(path, writeToStream));
        }

        public override void AddFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            _disableTfs = true;
            HashSet<string> filesToAdd = new HashSet<string>();
            try
            {
                foreach (var f in files)
                {
                    var path = GetFullPath(Path.Combine(rootDir, f.Path));
                    if (FileExists(path))
                    {
                        Logger.Log(MessageLevel.Warning, NuGetResources.Warning_FileAlreadyExists, path);
                    }

                    filesToAdd.Add(path);
                    filesToAdd.Add(Path.GetDirectoryName(path));

                    using (Stream stream = f.GetStream())
                    {
                        AddFile(path, stream);
                    }
                }
            }
            finally
            {
                _disableTfs = false;
            }

            Workspace.PendAdd(filesToAdd);
        }

        private void AddFileCore(string path, Action addFile)
        {
            if (!_disableTfs)
            {
                string fullPath = GetFullPath(path);

                // See if there are any pending changes for this file
                var pendingChanges = Workspace.GetPendingChanges(fullPath, RecursionType.None).ToArray();
                var pendingDeletes = pendingChanges.Where(c => c.IsDelete);

                // We would need to pend an edit if (a) the file is pending delete (b) is bound to source control and does not already have pending edits or adds
                bool sourceControlBound = IsSourceControlBound(path);
                bool requiresEdit = pendingDeletes.Any() || (!pendingChanges.Any(c => c.IsEdit || c.IsAdd) && sourceControlBound);

                // Undo all pending deletes
                Workspace.Undo(pendingDeletes);

                // If the file was marked as deleted, and we undid the change or has no pending adds or edits, we need to edit it.
                if (requiresEdit)
                {
                    // If the file exists, but there is not pending edit then edit the file (if it is under source control)
                    requiresEdit = Workspace.PendEdit(fullPath);
                }


                // Write to the underlying file system.
                addFile();

                // If we didn't have to edit the file, this must be a new file.
                if (!sourceControlBound)
                {
                    Workspace.PendAdd(fullPath);
                }
            }
            else
            {
                addFile();
            }
        }

        public override Stream CreateFile(string path)
        {
            string fullPath = GetFullPath(path);

            if (!base.FileExists(path))
            {
                // if this file doesn't exist, it's a new file
                Stream stream = base.CreateFile(path);
                Workspace.PendAdd(fullPath);
                return stream;
            }
            else
            {
                // otherwise it's an edit.

                bool requiresEdit = false;

                bool sourceControlBound = IsSourceControlBound(path);
                if (sourceControlBound)
                {
                    // only pend edit if the file is not already in edit state
                    var pendingChanges = Workspace.GetPendingChanges(fullPath, RecursionType.None);
                    requiresEdit = !pendingChanges.Any(c => c.IsEdit || c.IsAdd);
                }

                if (requiresEdit)
                {
                    Workspace.PendEdit(fullPath);
                }

                return base.CreateFile(path);
            }
        }

        public override void DeleteFile(string path)
        {
            string fullPath = GetFullPath(path);
            if (!DeleteItem(fullPath, RecursionType.None))
            {
                // If this file wasn't deleted, call base to remove it from the disk
                base.DeleteFile(path);
            }
        }

        public override void DeleteFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            HashSet<string> filesToPendDelete = new HashSet<string>();
            foreach (var file in files)
            {
                var fullPath = GetFullPath(Path.Combine(rootDir, file.Path));
                if (FileExists(fullPath))
                {
                    if (FileSystemExtensions.ContentEqual(this, fullPath, file.GetStream))
                    {
                        filesToPendDelete.Add(fullPath);
                    }
                    else
                    {
                        Logger.Log(MessageLevel.Warning, NuGetResources.Warning_FileModified, fullPath);
                    }
                }
            }

            // undo pending changes
            List<ITfsPendingChange> pendingChanges = Workspace.GetPendingChanges(
                GetFullPath(rootDir), RecursionType.Full).ToList();
            Workspace.Undo(pendingChanges);

            foreach (var pendingChange in pendingChanges)
            {
                if (pendingChange.IsAdd)
                {
                    filesToPendDelete.Remove(pendingChange.LocalItem);
                    base.DeleteFile(pendingChange.LocalItem);
                }
            }

            Workspace.PendDelete(filesToPendDelete, RecursionType.None);
        }

        public bool BindToSourceControl(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths");
            }

            paths = paths.Select(GetFullPath);
            return Workspace.PendAdd(paths);
        }

        public bool IsSourceControlBound(string path)
        {
            return Workspace.ItemExists(GetFullPath(path));
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            if (!DeleteItem(path, RecursionType.None))
            {
                // If no files were deleted, call base to remove them from the disk
                base.DeleteDirectory(path, recursive);
            }
        }

        private bool DeleteItem(string path, RecursionType recursionType)
        {
            if (_disableTfs)
            {
                return false;
            }

            string fullPath = GetFullPath(path);

            var pendingChanges = Workspace.GetPendingChanges(fullPath);

            // If there are any pending deletes then do nothing
            if (pendingChanges.Any(c => c.IsDelete))
            {
                return true;
            }

            // Undo other pending changes
            Workspace.Undo(pendingChanges);
            return Workspace.PendDelete(fullPath, recursionType);
        }

        protected override void EnsureDirectory(string path)
        {
            base.EnsureDirectory(path);

            if (!_disableTfs)
            {
                Workspace.PendAdd(GetFullPath(path));
            }
        }

        public void BeginProcessing(IEnumerable<string> batch, PackageAction action)
        {
            if (batch == null)
            {
                throw new ArgumentNullException("batch");
            }

            if (action == PackageAction.Install)
            {
                if (!batch.Any())
                {
                    // Short-circuit if nothing specified
                    return;
                }

                var batchSet = new HashSet<string>(batch.Select(GetFullPath), StringComparer.OrdinalIgnoreCase);
                var batchFolders = batchSet.Select(Path.GetDirectoryName)
                                      .Distinct()
                                      .ToArray();

                // Prior to installing, we'll look at the directories and make sure none of them have any pending deletes.
                var pendingDeletes = Workspace.GetPendingChanges(Root, RecursionType.Full)
                                              .Where(c => c.IsDelete);

                // Find pending deletes that are in the same path as any of the folders we are going to be adding.
                var pendingDeletesToUndo = pendingDeletes.Where(delete => batchFolders.Any(f => PathUtility.IsSubdirectory(delete.LocalItem, f)))
                                                         .ToArray();

                // Undo deletes.
                Workspace.Undo(pendingDeletesToUndo);

                // Expand the directory deletes into individual file deletes. Include all the files we want to add but exclude any directories that may be in the path of the file.
                var childrenToPendDelete = (from folder in pendingDeletesToUndo
                                            from childItem in Workspace.GetItemsRecursive(folder.LocalItem)
                                            where batchSet.Contains(childItem) || !batchFolders.Any(f => PathUtility.IsSubdirectory(childItem, f))
                                            select childItem).ToArray();
                Workspace.PendDelete(childrenToPendDelete, RecursionType.None);
            }
        }

        public void EndProcessing()
        {
            // Do nothing. All operations taken care of at the beginning of batch processing.
        }
    }
}
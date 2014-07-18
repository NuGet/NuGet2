using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Tools
{
    public class PackageManagerDocData : IVsPersistDocData
    {
        private Project _project;

        private readonly Guid _guidEditorType = new Guid(GuidList.guidEditorFactoryString);

        public Project Project
        {
            get
            {
                return _project;
            }
        }

        public IVsPackageSourceProvider PackageSourceProvider
        {
            get;
            private set;
        }

        IPackageRepositoryFactory _packageRepoFactory;

        public IVsPackageManagerFactory PackageManagerFactory { get; private set; }
        public IPackageRepository LocalRepo { get; private set; }

        public IPackageRepository ActiveSourceRepo { get; private set; }

        public IVsPackageManager PackageManager
        {
            get
            {
                return PackageManagerFactory.CreatePackageManager(
                    ActiveSourceRepo, useFallbackForDependencies: true);
            }
        }

        
        public PackageManagerDocData(Project project)
        {
            PackageSourceProvider = ServiceLocator.GetInstance<IVsPackageSourceProvider>();
            _packageRepoFactory = ServiceLocator.GetInstance<IPackageRepositoryFactory>();
            PackageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            ActiveSourceRepo = _packageRepoFactory.CreateRepository(PackageSourceProvider.ActivePackageSource.Name);

            _project = project;

            var packageManager = PackageManagerFactory.CreatePackageManagerToManageInstalledPackages();
            var projectManager = packageManager.GetProjectManager(_project);
            LocalRepo = projectManager.LocalRepository;
        }

        public void ChangeActiveSourceRepo(string name)
        {
            PackageSourceProvider.ActivePackageSource = PackageSourceProvider.GetEnabledPackageSourcesWithAggregate()
                .FirstOrDefault(ps => ps.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            
            ActiveSourceRepo = _packageRepoFactory.CreateRepository(PackageSourceProvider.ActivePackageSource.Name);
        }

        public IEnumerable<string> GetEnabledPackageSourcesWithAggregate()
        {
            return PackageSourceProvider.GetEnabledPackageSourcesWithAggregate().Select(ps => ps.Name);
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

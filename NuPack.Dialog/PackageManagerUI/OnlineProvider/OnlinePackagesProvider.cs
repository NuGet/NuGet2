using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuPack.Dialog.ToolsOptionsUI;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of extensions from the extension repository
    /// which will be shown in the Add NuPack dialog.
    /// </summary>
    internal class OnlinePackagesProvider : VsExtensionsProvider, IVsProgressPaneConsumer {
        private IVsExtensionsTreeNode m_SearchNode = null;
        private bool m_OnlineDisabled;
        private Dispatcher m_CurrentDispatcher;
        private ResourceDictionary _resources;
        protected NuPack.VisualStudio.VSPackageManager _vsPackageManager;
        protected EnvDTE.DTE _dte;
        protected EnvDTE.Project _project;
        protected NuPack.ProjectManager _vsProjectManager;
        protected string _feed;
        protected IPackageRepository _packagesRepository;

        public OnlinePackagesProvider(ResourceDictionary resources, bool onlineDisabled) {
            _resources = resources;
            m_OnlineDisabled = onlineDisabled;
        }


        private IVsProgressPane ProgressPane { get; set; }

        protected virtual IPackageRepository PackagesRepository {
            get {
                if (_packagesRepository == null) {
                    _feed = Settings.RepositoryServiceUri;
                    _dte = Utilities.ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                    _project = GetActiveProject(_dte);

                    if (String.IsNullOrEmpty(_feed)) {
                        return EmptyPackageRepository.Default;
                    }

                    _vsPackageManager = NuPack.VisualStudio.VSPackageManager.GetPackageManager(_feed, _dte);
                    _vsProjectManager = _vsPackageManager.GetProjectManager(_project);

                    _packagesRepository = _vsPackageManager.ExternalRepository;

                }
                return _packagesRepository;
            }
        }

        public virtual IQueryable<Package> GetQuery() {
            return PackagesRepository.GetPackages();
        }

        internal static EnvDTE.Project GetActiveProject(EnvDTE._DTE dte) {
            EnvDTE.Project activeProject = null;

            if (dte != null) {
                Object obj = dte.ActiveSolutionProjects;
                if (obj != null && obj is Array && ((Array)obj).Length > 0) {
                    Object proj = ((Array)obj).GetValue(0);

                    if (proj != null && proj is EnvDTE.Project) {
                        activeProject = (EnvDTE.Project)proj;
                    }
                }
            }
            return activeProject;
        }

        /// <summary>
        /// Gets the root node of the tree
        /// </summary>
        protected virtual IVsExtensionsTreeNode RootNode {
            get;
            set;
        }

        public override IVsExtensionsTreeNode ExtensionsTree {
            get {
                if (RootNode == null) {
                    this.m_CurrentDispatcher = Dispatcher.CurrentDispatcher;
                    RootNode = new BasePackagesTree(null, String.Empty);
                    new Thread(new ThreadStart(CreateExtensionsTree)).Start();
                }

                return RootNode;
            }
        }

        private object _mediumIconDataTemplate;
        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["OnlineTileTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        private object _detailViewDataTemplate;
        public override object DetailViewDataTemplate {
            get {
                if (_detailViewDataTemplate == null) {
                    _detailViewDataTemplate = _resources["OnlineDetailTemplate"];
                }
                return _detailViewDataTemplate;
            }
        }

        public override string Name {
            get {
                return "Online";
            }
        }

        public override string ToString() {
            return Name;
        }

        public override IVsExtensionsTreeNode Search(string searchTerms) {
            if (m_OnlineDisabled) return null;

            if (m_SearchNode != null) {
                // dispose any search results
                this.RootNode.Nodes.Remove(m_SearchNode);
                m_SearchNode = null;
            }

            if (!string.IsNullOrEmpty(searchTerms)) {
                m_SearchNode = new OnlinePackagesSearchNode(this, this.PackagesRepository, this.RootNode, searchTerms);
                m_SearchNode.IsSelected = true;
                ExtensionsTree.Nodes.Add(m_SearchNode);
            }

            return m_SearchNode;
        }

        #region IVsProgressPaneConsumer Members

        void IVsProgressPaneConsumer.SetProgressPane(IVsProgressPane progressPane) {
            this.ProgressPane = progressPane;
        }

        #endregion

        private string urlNeedingCredentials;

        private void CreateExtensionsTree() {
            this.m_CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate() {
                if (this.ProgressPane != null && this.ProgressPane.Show(null, false)) {
                    this.ProgressPane.IsIndeterminate = true;
                }
            }));

            List<IVsExtensionsTreeNode> rootNodes = new List<IVsExtensionsTreeNode>();
            if (this.ProgressPane != null) {
                this.ProgressPane.Close();
            }

            if (this.m_CurrentDispatcher != null) {
                this.m_CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new ThreadStart(delegate() {
                        //The user may have done a search before we finished getting the category list.
                        //temporarily remove it
                        if (m_SearchNode != null) {
                            RootNode.Nodes.Remove(m_SearchNode);
                        }

                        //Add the special "All" node which doesn't filter by category.
                        RootNode.Nodes.Add(new OnlinePackagesTree(this, PackagesRepository, "All", RootNode));

                        rootNodes.ForEach(node => RootNode.Nodes.Add(node));

                        if (m_SearchNode != null) {
                            //Re-add the search node and select it if the user was doing a search
                            RootNode.Nodes.Add(m_SearchNode);
                            m_SearchNode.IsSelected = true;
                        }
                        else {
                            //If they weren't doing a search, select the first category.
                            RootNode.Nodes.First().IsSelected = true;
                        }
                    }
                ));
            }
        }

        internal static string CurrentLocale {
            get {
                return System.Globalization.CultureInfo.CurrentCulture.Name.ToLowerInvariant();
            }
        }

        public void Install(string id, Version version) {
            _vsProjectManager.AddPackageReference(id, version);
        }

        public bool IsInstalled(string id) {
            return (_vsProjectManager.GetPackageReference(id) != null);
        }
    }
}

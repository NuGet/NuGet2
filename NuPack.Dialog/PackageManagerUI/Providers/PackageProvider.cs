using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuPack.VisualStudio;

namespace NuPack.Dialog.PackageManagerUI.Providers {
    internal abstract class PackageProvider : VsExtensionsProvider {
        public static DateTime FirstRunOfPackageManager = DateTime.UtcNow;

        private string _providerName;
        private string _category;
        private PackageTreeNode _searchNode;
        private PackageTreeNode _packagesTree;
        private ObservableCollection<NuPack.IPackage> _packageRecords = new ObservableCollection<NuPack.IPackage>();
        private ConcurrentDictionary<string, PackageTreeNode> _treeNode = new ConcurrentDictionary<string, PackageTreeNode>(StringComparer.OrdinalIgnoreCase);
        private ResourceDictionary _resources;
        private IVsProgressPane _progressPane;

        private EnvDTE.DTE _dte;
        private EnvDTE.Project _project;

        public PackageProvider(ResourceDictionary resources, IVsProgressPane progressPane, string providerName, string category) {
            _providerName = providerName;
            _category = category;
            _resources = resources;
            _progressPane = progressPane;
        }

        private EnvDTE.DTE DTE {
            get {
                // REVIEW: Is it safe to cache this?
                if (_dte == null) {
                    _dte = Utilities.ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                }
                return _dte;
            }
        }

        protected VSPackageManager PackageManager {
            get {
                return VSPackageManager.GetPackageManager(DTE);
            }
        }

        private EnvDTE.Project Project {
            get {
                if (_project == null) {
                    _project = Utilities.GetActiveProject(DTE);
                }
                return _project;
            }
        }

        protected ProjectManager ProjectManager {
            get {
                return PackageManager.GetProjectManager(Project);
            }
        }

        /// <summary>
        /// The node that will contain the search results
        /// </summary>
        internal PackageTreeNode SearchResultsNode {
            get { return _searchNode; }
            set { _searchNode = value; }
        }

        /// <summary>
        /// The Resources for the hosting dialog
        /// </summary>
        public ResourceDictionary Resources {
            get { return _resources; }
        }

        /// <summary>
        /// The set of package records that the dialog will data bind to
        /// </summary>
        public ObservableCollection<NuPack.IPackage> PackageRecords {
            get { return _packageRecords; }
        }

        /// <summary>
        /// Top level name of this provider
        /// </summary>
        public override string Name {
            get { return _providerName; }
        }

        /// <summary>
        /// First level category for this provider
        /// </summary>
        public virtual string Category {
            get { return _category; }
        }

        /// <summary>
        /// List template for this package type
        /// </summary>
        private object _mediumIconDataTemplate;

        /// <summary>
        /// List template for this package type
        /// </summary>

        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["MediumIconTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }


        /// <summary>
        /// The tree of extensions that will be shown on the left of the dialog
        /// </summary>
        public override IVsExtensionsTreeNode ExtensionsTree {
            get {
                if (_packagesTree == null) {
                    _packagesTree = new PackageTreeNode();
                    _packagesTree.IsExpanded = true;
                    _packagesTree.IsSelected = true;
                    _packagesTree.Name = Name;

                    PackageTreeNode categoryNode = new PackageTreeNode();
                    categoryNode.Name = Category;
                    categoryNode.Parent = _packagesTree;
                    categoryNode.IsExpanded = false;
                    _packagesTree.Nodes.Add(categoryNode);
                    _treeNode[Category] = categoryNode;

                    foreach (NuPack.IPackage rec in _packageRecords) {
                        AddRecordToCategoryNode(rec);
                    }
                }
                return _packagesTree;
            }
        }

        /// <summary>
        /// Show the progress pane if we have been handed one
        /// </summary>
        public void ShowProgressPane() {
            if (_progressPane != null) {
                _progressPane.Show(new CancelProgressCallback(CancelPopulation), true);
            }
        }

        /// <summary>
        /// Close the progress pane if we have been handed one
        /// </summary>
        public void CloseProgressPane() {
            if (_progressPane != null) {
                _progressPane.Close();
            }
        }

        /// <summary>
        /// Cancel the population of packages (not supported)
        /// </summary>
        private void CancelPopulation() {
            // No cancellation support
        }

        /// <summary>
        /// Add the package record to the category
        /// </summary>
        /// <param name="rec"></param>
        private void RemoveRecordFromCategoryNode(NuPack.IPackage rec) {
            PackageTreeNode subCategoryNode;

            if (_treeNode.TryGetValue(rec.Category, out subCategoryNode)) {
                IVsExtension packageItem = (PackageListItem)subCategoryNode.Extensions.FirstOrDefault(package => (package as PackageListItem).Id == rec.Id);

                if (packageItem != null) {
                    subCategoryNode.Extensions.Remove(packageItem);
                }
            }
        }

        /// <summary>
        /// Add the package record to the category
        /// </summary>
        /// <param name="rec"></param>
        private void AddRecordToCategoryNode(NuPack.IPackage rec) {
            PackageTreeNode subCategoryNode;
            string category = _category;  //rec.Category

            if (!_treeNode.TryGetValue(category, out subCategoryNode)) {
                subCategoryNode = new PackageTreeNode();
                subCategoryNode.Name = category;
                subCategoryNode.Parent = _packagesTree;
                subCategoryNode.IsExpanded = false;
                _packagesTree.Nodes.Add(subCategoryNode);
                _treeNode[category] = subCategoryNode;
            }

            subCategoryNode.Extensions.Add(new PackageListItem(rec, false, null, 1, null));
        }

        /// <summary>
        /// In order to support async population, we need to hable the collection changed event
        /// so that the records can be reflected in the dialog
        /// </summary>
        void _packageRecords_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (_packagesTree != null) {
                // Add new items to the extension records tree
                if (e.NewItems != null) {
                    foreach (NuPack.IPackage rec in e.NewItems) {
                        AddRecordToCategoryNode(rec);
                    }
                }

                // Remove old items from the extension records tree
                if (e.OldItems != null) {
                    foreach (NuPack.IPackage rec in e.OldItems) {
                        RemoveRecordFromCategoryNode(rec);
                    }
                }
            }
        }

        /// <summary>
        /// Very simple searching support
        /// </summary>
        public override IVsExtensionsTreeNode Search(string searchTerms) {
            if (SearchResultsNode != null) {
                // dispose any search results
                //
                this.ExtensionsTree.Nodes.Remove(SearchResultsNode);

                SearchResultsNode = null;
            }

            if (!string.IsNullOrEmpty(searchTerms)) {
                CreateSearchResultsNode();

                SearchNode(ExtensionsTree, ParseSearchTerms(searchTerms));

                SearchResultsNode.IsSelected = true;
                SearchResultsNode.IsExpanded = true;
                ExtensionsTree.Nodes.Add(SearchResultsNode);
            }

            return SearchResultsNode;
        }

        private void CreateSearchResultsNode() {
            SearchResultsNode = new PackageTreeNode();
            SearchResultsNode.Name = "Search Results";
            SearchResultsNode.Parent = _packagesTree;
            SearchResultsNode.IsSelected = true;
        }

        /// <summary>
        /// Parses search terms
        /// </summary>
        /// <param name="searchTerms">Input search terms string from user</param>
        /// <returns>Array of strings to search</returns>
        private string[] ParseSearchTerms(string searchTerms) {
            if (searchTerms == null) throw new ArgumentNullException("searchTerms");

            string[] inputTerms = searchTerms.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            return inputTerms;
        }

        /// <summary>
        /// Search a tree node for records that matches terms and add them into my search node.
        /// </summary>
        private void SearchNode(IVsExtensionsTreeNode node, string[] terms) {
            if (node == null || terms == null || terms.Length == 0) return;

            if (node.Extensions != null) {
                foreach (PackageListItem packageItem in node.Extensions) {
                    if (!_searchNode.Extensions.Contains(packageItem) && IsMatch(packageItem.ExtensionRecord, terms)) {
                        _searchNode.Extensions.Add(new PackageListItem(packageItem.ExtensionRecord, false, null, 1, null));
                    }
                }
            }

            if (node.Nodes != null) {
                foreach (IVsExtensionsTreeNode subNode in node.Nodes) {
                    SearchNode(subNode, terms);
                }
            }
        }

        /// <summary>
        /// Check if all terms are found somewhere in a record.
        /// </summary>
        private static bool IsMatch(NuPack.IPackage record, string[] terms) {
            foreach (var _term in terms) {
                if (!IsMatch(record, _term)) {
                    return false; // a term not found in the record
                }
            }

            return true; // all terms found in the record
        }

        /// <summary>
        /// Check if a given term is found somewhere in a record.
        /// </summary>
        private static bool IsMatch(NuPack.IPackage record, string term) {
            return IsMatch(record.Id, term)
                || IsMatch(record.Description, term)
                || IsMatch(record.Version.ToString(), term)
                || IsMatch(record.Category, term);
        }

        /// <summary>
        /// Check if a term is found in given content.
        /// </summary>
        private static bool IsMatch(string content, string term) {
            return !string.IsNullOrEmpty(content) && content.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

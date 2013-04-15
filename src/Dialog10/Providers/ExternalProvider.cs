using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    public class ExternalProvider : VsExtensionsProvider
    {
        private readonly string _name;
        private readonly IVsExtensionsTreeNode _rootTreeNode;
        private readonly ResourceDictionary _resources;
        private object _mediumIconDataTemplate;
        private object _detailViewDataTemplate;

        public ExternalProvider(ResourceDictionary resources, string name, IList<IPackageProvider> packageProviders)
        {
            _resources = resources;
            _name = name;
            _rootTreeNode = new RootPackagesTreeNode();
            _rootTreeNode.Nodes.Add(new ExternalPackagesTreeNode(packageProviders));
        }

        public override string Name
        {
            get 
            {
                return _name;
            }
        }

        public override IVsExtensionsTreeNode ExtensionsTree
        {
            get 
            {
                return _rootTreeNode;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 4.0f;
            }
        }

        public override object MediumIconDataTemplate
        {
            get
            {
                if (_mediumIconDataTemplate == null)
                {
                    _mediumIconDataTemplate = _resources["SuggestedPackageItemTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        public override object DetailViewDataTemplate
        {
            get
            {
                if (_detailViewDataTemplate == null)
                {
                    _detailViewDataTemplate = _resources["SuggestedPackageDetailTemplate"];
                }
                return _detailViewDataTemplate;
            }
        }
    }
}
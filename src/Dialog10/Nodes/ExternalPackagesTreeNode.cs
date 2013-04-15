using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    internal class ExternalPackagesTreeNode : IVsExtensionsTreeNode
    {
#if VS10
        private readonly IList<IVsExtension> _extensions;
#else
        private readonly IList _extensions;
#endif

        public ExternalPackagesTreeNode(IList<IPackageProvider> packageProviders)
        {
            _extensions = packageProviders.Select(p => (IVsExtension)new SuggestedPackage(p)).ToList();
        }

#if VS10
        public IList<IVsExtension> Extensions
#else
        public IList Extensions 
#endif
        {
            get { return _extensions; }
        }

        public bool IsExpanded
        {
            get;
            set;
        }

        public bool IsSearchResultsNode
        {
            get { return false; }
        }

        public bool IsSelected
        {
            get;
            set;
        }

        public string Name
        {
            get { return "All"; }
        }

        public IList<IVsExtensionsTreeNode> Nodes
        {
            // no child node
            get { return new IVsExtensionsTreeNode[0]; }
        }

        public IVsExtensionsTreeNode Parent
        {
            get { return null; }
        }
    }
}

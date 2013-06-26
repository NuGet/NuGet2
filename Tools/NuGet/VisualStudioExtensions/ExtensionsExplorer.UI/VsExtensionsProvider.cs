namespace Microsoft.VisualStudio.ExtensionsExplorer.UI
{
    using Microsoft.VisualStudio.ExtensionsExplorer;
    using System;

    public abstract class VsExtensionsProvider : IVsExtensionsProvider
    {
        protected VsExtensionsProvider()
        {
        }

        public virtual IVsExtensionsTreeNode Search(string searchTerms)
        {
            return null;
        }

        public virtual object DetailViewDataTemplate
        {
            get
            {
                return null;
            }
        }

        public abstract IVsExtensionsTreeNode ExtensionsTree { get; }

        public virtual object HeaderContent
        {
            get
            {
                return null;
            }
        }

        public virtual object ItemContainerStyle
        {
            get
            {
                return null;
            }
        }

        public virtual object LargeIconDataTemplate
        {
            get
            {
                return null;
            }
        }

        public virtual bool ListMultiSelect
        {
            get
            {
                return false;
            }
        }

        public virtual bool ListVisibility
        {
            get
            {
                return true;
            }
        }

        public virtual object MediumIconDataTemplate
        {
            get
            {
                return null;
            }
        }

        public abstract string Name { get; }

        public virtual object SmallIconDataTemplate
        {
            get
            {
                return null;
            }
        }

        public virtual float SortOrder
        {
            get
            {
                return 100f;
            }
        }

        public virtual object View
        {
            get
            {
                return null;
            }
        }
    }
}


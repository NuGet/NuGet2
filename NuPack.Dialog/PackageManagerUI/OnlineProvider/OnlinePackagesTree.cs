using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal class OnlinePackagesTree : OnlinePackagesTreeBase {
        #region Private Members

        private string m_Name;
        private string Category { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor - requires provider
        /// </summary>
        /// <param name="provider">Instance of IVsTemplateProvider</param>
        public OnlinePackagesTree(OnlinePackagesProvider provider, IPackageRepository repository, string category, IVsExtensionsTreeNode parent) :
            base(repository, parent, provider) {
            if (category == null) throw new ArgumentNullException("category");
            this.Category = category;
        }

        #endregion

        public override string Name {
            get {
                if (m_Name == null) {
                    m_Name = Category;
                }
                return m_Name;
            }
        }

        protected override IQueryable<Package> PreviewQuery(IQueryable<Package> query) {
            return query;
        }

        protected override void FillNodes(IList<IVsExtensionsTreeNode> nodes) {

        }
    }
}

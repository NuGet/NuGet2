using System;
using System.ComponentModel.Composition;

namespace PackageExplorerViewModel.Types {
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
    public sealed class PackageContentViewerMetadataAttribute : ExportAttribute {

        private readonly string[] _supportedExtensions;
        private readonly int _order;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SupportedExtensions {
            get {
                return _supportedExtensions;
            }
        }

        public int Order {
            get {
                return _order;
            }
        }

        public PackageContentViewerMetadataAttribute(int order, params string[] supportedExtensions) : 
            base(typeof(IPackageContentViewer)) {
            _supportedExtensions = supportedExtensions;
            _order = order;
        }
    }
}
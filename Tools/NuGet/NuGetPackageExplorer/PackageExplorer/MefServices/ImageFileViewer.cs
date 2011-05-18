using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {
    [PackageContentViewerMetadata(1, ".jpg", ".gif", ".png")]
    internal class ImageFileViewer : IPackageContentViewer {
        public object GetView(Stream stream) {
            var source = new BitmapImage();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.StreamSource = stream;
            source.EndInit();

            return new Image {
                Source = source,
                Width = source.Width,
                Height = source.Height
            };
        }
    }
}
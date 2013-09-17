namespace Microsoft.VisualStudio.ExtensionsExplorer
{
    using System;
    using System.Windows.Media.Imaging;

    public interface IVsExtension
    {
        string Description { get; }

        string Id { get; }

        bool IsSelected { get; set; }

        BitmapSource MediumThumbnailImage { get; }

        string Name { get; }

        BitmapSource PreviewImage { get; }

        float Priority { get; }

        BitmapSource SmallThumbnailImage { get; }
    }
}


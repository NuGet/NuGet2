using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using NuGet;

namespace NuGet.WebMatrix
{
    internal static class Extensions
    {
        internal static PackageSource ToNuGetPackageSource(this FeedSource feedSource)
        {
            return new PackageSource(feedSource.SourceUrl.AbsoluteUri, feedSource.Name);
        }

        internal static FeedSource ToFeedSource(this PackageSource packageSource)
        {
            Uri uri;
            if (Uri.TryCreate(packageSource.Source, UriKind.Absolute, out uri))
            {
                return new FeedSource(uri, packageSource.Name);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Bitmap to an ImageSource. If the bitmap is null, returns null
        /// </summary>
        /// <param name="bitmap">A Bitmap instance.</param>
        /// <returns>Returns an ImageSource. If the Bitmap this extension method runs on is null, the return value is null.</returns>
        internal static ImageSource ConvertToImageSource(this Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }
            else
            {
                IntPtr hbitmap = bitmap.GetHbitmap();
                try
                {
                    // Convert image format.
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hbitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    // Free native resources
                    if (IntPtr.Zero != hbitmap)
                    {
                        NativeMethods.DeleteObject(hbitmap);
                    }
                }
            }
        }
    }
}

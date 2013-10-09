using System;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix
{
    public static class GalleryDescriptors
    {
        private static INuGetGalleryDescriptor _nuGet;

        public static INuGetGalleryDescriptor NuGet
        {
            get
            {
                if (_nuGet == null)
                {
                    _nuGet = new NuGetGalleryDescriptor()
                    {
                        DialogTitle = Resources.String_NuGetGalleryHeading,
                        FeedName = Resources.CuratedFeedSourceTitle,
                        FeedUri = new Uri(FWLink.GetLinkWithClcid(294092)),
                        FilterTag = null,
                        GalleryId = (int)NuGetGalleryId.NuGet,
                        LoadingMessage = Resources.String_LoadingNuGetGallery,
                        PackageKind = Resources.Notification_NugetPackage,
                        PreferencesStore = new Guid("72E7412B-A156-4637-B22B-721E0E2BD29E"),
                    };
                }

                return _nuGet;
            }
        }
    }
}

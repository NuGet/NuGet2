namespace NuPack {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using Microsoft.Internal.Web.Utils;

    public static class PackageRepositoryFactory {
        private const string NuPackVersionHeader = "NuPackVersion";

        public static IPackageRepository CreateRepository(string feedOrPath) {
            if (String.IsNullOrEmpty(feedOrPath)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "feedOrPath");
            }

            if (IsLocalPath(feedOrPath)) {
                return new LocalPackageRepository(feedOrPath);
            }
            return CreateFeedRepository(new Uri(feedOrPath));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", 
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification="We don't care about handling exception in this case.")]
        private static IPackageRepository CreateFeedRepository(Uri uri) {
            // HACK: We need a better way the feed version and choose the right client impl
            FeedType feedType = FeedType.Atom;
            try {
                // Send a request to the feed server asking what version it is
                WebRequest request = WebRequest.Create(uri);
                HttpWebRequestor.InitializeRequest(request);
                request.Headers[NuPackVersionHeader] = "true";
                WebResponse response = request.GetResponse();
                Version version;
                if (Version.TryParse(response.Headers[NuPackVersionHeader], out version)) {
                    feedType = (FeedType)version.Major;
                }
            }
            catch {
                feedType = FeedType.Atom;
            }

            switch (feedType) {
                case FeedType.Atom:
                    return new AtomFeedPackageRepository(uri);
                case FeedType.OData:
                    return new DataServicePackageRepository(uri);
            }

            Debug.Fail("Unknown feed type");
            return null;
        }

        private static bool IsLocalPath(string feedOrPath) {
            return Path.IsPathRooted(feedOrPath);
        }

        private enum FeedType {
            Atom = 1,
            OData = 2
        }
    }
}
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Internal.Web.Utils;

namespace NuPack {
    public class PackageRepositoryFactory : IPackageRepositoryFactory {
        private const string NuPackVersionHeader = "NuPackVersion";
        private static readonly IPackageRepositoryFactory _default = new PackageRepositoryFactory();

        public static IPackageRepositoryFactory Default {
            get {
                return _default;
            }
        }

        public IPackageRepository CreateRepository(string source) {
            if (String.IsNullOrEmpty(source)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "source");
            }

            Uri uri = new Uri(source);
            if (uri.IsFile) {
                return new LocalPackageRepository(uri.LocalPath);
            }
            return CreateFeedRepository(uri);
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't care about handling exception in this case.")]
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

        private enum FeedType {
            Atom = 1,
            OData = 2
        }
    }
}
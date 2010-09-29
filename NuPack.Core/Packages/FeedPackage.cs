namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;

    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Our implementation of PackageProperties has nothing to dispose")]
    internal class FeedPackage : LazyPackage {
        private PackageSyndicationItem _item;

        public FeedPackage(PackageSyndicationItem item) {
            Debug.Assert(item != null, "item should not be null");
            _item = item;
        }

        public override string Id {
            get {
                return _item.PackageId;
            }
        }

        public override IEnumerable<PackageDependency> Dependencies {
            get {
                return _item.Dependencies;
            }
        }

        public override Version Version {
            get {
                return _item.Version;
            }
        }

        public override IEnumerable<string> Authors {
            get {
                return _item.Authors.Select(a => a.Name);
            }
        }

        public override string Category {
            get {
                if (_item.Categories.Count > 0) {
                    return _item.Categories[0].Name;
                }
                return null;
            }
        }

        public override string Description {
            get {
                return _item.Description;
            }
        }

        public override DateTime Created {
            get {
                return _item.PublishDate.DateTime;
            }
        }

        public override IEnumerable<string> Keywords {
            get {
                return _item.Keywords;
            }
        }

        public override string Language {
            get {
                return _item.Language;
            }
        }

        public override DateTime Modified {
            get {
                return _item.LastUpdatedTime.DateTime;
            }
        }

        public override string LastModifiedBy {
            get {
                return _item.LastModifiedBy;
            }
        }

        public override bool RequireLicenseAcceptance {
            get {
                return _item.RequireLicenseAcceptance;
            }
        }

        public override Uri LicenseUrl {
            get {
                if (_item.LicenseLink != null) {
                    return _item.LicenseLink.Uri;
                }
                return null;
            }
        }

        protected override IPackage CreatePackage() {
            // REVIEW: Should we be using WebClient?
            using (var client = new WebClient()) {
                // Make sure we use the default credentials for this request
                client.UseDefaultCredentials = true;
                Utility.ConfigureProxy(client.Proxy, _item.DownloadLink.Uri);
                // TODO: Verify package hash and length
                byte[] rawPackage = client.DownloadData(_item.DownloadLink.Uri);
                using (var stream = new MemoryStream(rawPackage)) {
                    return new ZipPackage(stream);
                }
            }
        }
    }
}

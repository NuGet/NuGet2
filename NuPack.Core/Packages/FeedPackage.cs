namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

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

        protected override Package CreatePackage() {
            return WebClientUtility.DownloadPackage(_item.SourceUrl);
        }
    }
}

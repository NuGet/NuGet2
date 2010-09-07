namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.ServiceModel.Syndication;
    using System.Xml;
    using System.Xml.Linq;

    public class PackageSyndicationFeed : SyndicationFeed {
        private const string PackageXmlNamespace = "pkg";

        protected override SyndicationItem CreateItem() {
            return new PackageSyndicationItem();
        }

        public static SyndicationFeed Create(string physicalPath, Func<Package, Uri> uriSelector) {
            return Create(new LocalPackageRepository(physicalPath), uriSelector);
        }

        public static SyndicationFeed Create(IPackageRepository repository, Func<Package, Uri> uriSelector) {
            var items = new List<SyndicationItem>();
            foreach (var package in repository.GetPackages()) {
                // REVIEW: We should to change this format to a valid URI                
                string entryId = String.Format(CultureInfo.InvariantCulture, "urn:uuid:{0:d}", Guid.NewGuid());

                // Set the description from the package core properties
                var description = new TextSyndicationContent(package.Description);

                // TODO: Find cleaner way to to the link uri for a package
                var item = new SyndicationItem(package.Id,
                                               description,
                                               uriSelector(package),
                                               entryId,
                                               package.Modified);

                // Setup the link for the download
                SyndicationLink downloadLink = item.Links[0];
                downloadLink.RelationshipType = "enclosure";

                // TODO: Move this code outside of auto feed generation
                using (var stream = new MemoryStream()) {
                    PackageBuilder.Save(package, stream);
                    downloadLink.Length = stream.Length;
                    var hashAttributeName = new XmlQualifiedName("hash");
                    downloadLink.AttributeExtensions[hashAttributeName] = "sha-512:" + GetHash(stream);
                }

                foreach (var author in package.Authors) {
                    item.Authors.Add(new SyndicationPerson {
                        Name = author
                    });
                }

                if (!String.IsNullOrEmpty(package.Category)) {
                    item.Categories.Add(new SyndicationCategory(package.Category));
                }

                // Set the publish date
                item.PublishDate = package.Created;

                // Add our custom extensions with our namespace
                item.ElementExtensions.Add("packageId", Package.SchemaNamespace, package.Id);
                item.ElementExtensions.Add("version", Package.SchemaNamespace, package.Version.ToString());

                if (!String.IsNullOrEmpty(package.Language)) {
                    item.ElementExtensions.Add("language", Package.SchemaNamespace, package.Language);
                }

                if (!String.IsNullOrEmpty(package.LastModifiedBy)) {
                    item.ElementExtensions.Add("lastModifiedBy", Package.SchemaNamespace, package.LastModifiedBy);
                }

                if (package.Keywords.Any()) {
                    item.ElementExtensions.Add("keywords", Package.SchemaNamespace, package.Keywords.ToArray());
                }

                if (package.Dependencies.Any()) {
                    var dependencies = from dependency in package.Dependencies
                                       select new PackageFeedDependency(dependency);

                    item.ElementExtensions.Add("dependencies", Package.SchemaNamespace, dependencies.ToArray());
                }

                items.Add(item);
            }

            var feed = new SyndicationFeed(items);
            // Add package as a top level namespace in the feed
            feed.AttributeExtensions.Add(new XmlQualifiedName(PackageXmlNamespace, XNamespace.Xmlns.NamespaceName), Package.SchemaNamespace);
            return feed;
        }

        private static string GetHash(Stream stream) {
            using (var hashAlgorithm = SHA512.Create()) {
                return String.Join(String.Empty, hashAlgorithm.ComputeHash(stream)
                                                              .Select(b => b.ToString("x2")));
            }
        }
    }
}

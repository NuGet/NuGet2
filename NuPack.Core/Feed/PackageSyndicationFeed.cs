namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel.Syndication;
    using System.Xml;
    using System.Xml.Linq;

    public class PackageSyndicationFeed : SyndicationFeed {
        private const string PackageXmlNamespace = "pkg";

        protected override SyndicationItem CreateItem() {
            return new PackageSyndicationItem();
        }

        public static SyndicationFeed Create(string physicalPath, Func<IPackage, Uri> uriSelector) {
            return Create(new LocalPackageRepository(physicalPath), uriSelector);
        }

        public static SyndicationFeed Create(IPackageRepository repository, Func<IPackage, Uri> uriSelector) {
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
               
                // Add the license url link if the package specifies one
                if (package.LicenseUrl != null) {
                    item.Links.Add(new SyndicationLink(package.LicenseUrl) {
                        RelationshipType = "license"
                    });
                }

                foreach (var author in package.Authors) {
                    item.Authors.Add(new SyndicationPerson {
                        Name = author
                    });
                }

                if (!String.IsNullOrEmpty(package.Category)) {
                    item.Categories.Add(new SyndicationCategory(package.Category));
                }


                // Add the RequireLicenseAcceptance bit
                item.ElementExtensions.Add("requireLicenseAcceptance", Constants.SchemaNamespace, package.RequireLicenseAcceptance);

                // Set the publish date
                item.PublishDate = package.Created;

                // Add our custom extensions with our namespace
                item.ElementExtensions.Add("packageId", Constants.SchemaNamespace, package.Id);
                item.ElementExtensions.Add("version", Constants.SchemaNamespace, package.Version.ToString());

                if (!String.IsNullOrEmpty(package.Language)) {
                    item.ElementExtensions.Add("language", Constants.SchemaNamespace, package.Language);
                }

                if (!String.IsNullOrEmpty(package.LastModifiedBy)) {
                    item.ElementExtensions.Add("lastModifiedBy", Constants.SchemaNamespace, package.LastModifiedBy);
                }

                if (package.Keywords.Any()) {
                    item.ElementExtensions.Add("keywords", Constants.SchemaNamespace, package.Keywords.ToArray());
                }

                if (package.Dependencies.Any()) {
                    var dependencies = from dependency in package.Dependencies
                                       select new PackageFeedDependency(dependency);

                    item.ElementExtensions.Add("dependencies", Constants.SchemaNamespace, dependencies.ToArray());
                }

                items.Add(item);
            }

            var feed = new SyndicationFeed(items);
            // Add package as a top level namespace in the feed
            feed.AttributeExtensions.Add(new XmlQualifiedName(PackageXmlNamespace, XNamespace.Xmlns.NamespaceName), Constants.SchemaNamespace);
            return feed;
        }
    }
}

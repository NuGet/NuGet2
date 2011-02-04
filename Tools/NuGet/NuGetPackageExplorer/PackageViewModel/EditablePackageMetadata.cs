using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel {

    public class EditablePackageMetadata : IPackageMetadata {
        public EditablePackageMetadata() {
        }

        public EditablePackageMetadata(IPackageMetadata source) {
            CopyFrom(source);
        }

        public void CopyFrom(IPackageMetadata source) {
            this.Id = source.Id;
            this.Version = source.Version;
            this.Title = source.Title;
            //this.Authors = source.Authors;
            //this.Owners = source.Owners;
            this.IconUrl = source.IconUrl;
            this.LicenseUrl = source.LicenseUrl;
            this.ProjectUrl = source.ProjectUrl;
            this.RequireLicenseAcceptance = source.RequireLicenseAcceptance;
            this.Description = source.Description;
            this.Summary = source.Summary;
            this.Language = source.Language;
            this.Tags = source.Tags;
            //this.Dependencies = source.Dependencies;
        }

        public string Id { get; set; }
        public Version Version { get; set; }
        public string Title { get; set; }
        public string Authors { get; private set; }
        public string Owners { get; private set; }
        public Uri IconUrl { get; set; }
        public Uri LicenseUrl { get; set; }
        public Uri ProjectUrl { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public string Language { get; set; }
        public string Tags { get; set; }
        public IList<PackageDependency> Dependencies { get; private set; }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                string authors = this.Authors;
                return authors == null ? Enumerable.Empty<string>() : authors.Split(',');
            }
        }

        IEnumerable<string> IPackageMetadata.Owners {
            get {
                string owners = this.Owners;
                return owners == null ? Enumerable.Empty<string>() : owners.Split(',');
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                return this.Dependencies;
            }
        }

        public override string ToString() {
            return Id + " " + Version.ToString();
        }
    }
}
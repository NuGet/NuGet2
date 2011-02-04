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
            this.Authors = ConvertToString(source.Authors);
            this.Owners = ConvertToString(source.Owners);
            this.IconUrl = source.IconUrl;
            this.LicenseUrl = source.LicenseUrl;
            this.ProjectUrl = source.ProjectUrl;
            this.RequireLicenseAcceptance = source.RequireLicenseAcceptance;
            this.Description = source.Description;
            this.Summary = source.Summary;
            this.Language = source.Language;
            this.Tags = source.Tags;
            this.Dependencies = new List<PackageDependency>(source.Dependencies);
        }

        public string Id { get; set; }
        public Version Version { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Owners { get; set; }
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
                return SplitString(this.Authors);
            }
        }

        IEnumerable<string> IPackageMetadata.Owners {
            get {
                return SplitString(this.Owners);
            }
        }

        private IEnumerable<string> SplitString(string text) {
            return text == null ? Enumerable.Empty<string>() : text.Split(',').Select(a => a.Trim());
        }

        private string ConvertToString(IEnumerable<string> items) {
            return String.Join(", ", items);
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
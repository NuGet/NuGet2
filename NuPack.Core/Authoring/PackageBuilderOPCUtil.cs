using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Opc = System.IO.Packaging;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace NuPack {
    public partial class PackageBuilder {
        private const string ApplicationPackagePrefix = "http://schemas.microsoft.com/net/package/2010/";
        private const string DefaultContentType = "application/octet";
        private const string ReferenceRelationType = "Reference";
        private const string DependencyRelationType = "Dependency";

        public void WritePackageContent(Stream stream) {
            using (Opc.Package package = Opc.Package.Open(stream, FileMode.Create)) {

                WriteReferences(package);
                WriteFiles(package);
                WriteDepdendencies(package);

                // Copy the metadata properties back to the package
                package.PackageProperties.Category = Category;
                package.PackageProperties.Created = Created;
                package.PackageProperties.Creator = String.Join(", ", Authors);
                package.PackageProperties.Description = Description;
                package.PackageProperties.Identifier = Id;
                package.PackageProperties.Version = Version.ToString();
            }
        }

        private void WriteDepdendencies(Opc.Package package) {
            XDocument doc = new XDocument();
            doc.Root.Add(
                from item in Dependencies
                select new XElement("Dependency", 
                    new XAttribute("id", item.Id), 
                    new XAttribute("version", item.Version),
                    new XAttribute("minversion", item.MinVersion), 
                    new XAttribute("maxversion", item.MaxVersion)
                )
            );

            Uri uri = UriHelper.CreatePartUri(ApplicationPackagePrefix + DependencyRelationType);

            // Create the relationship type
            package.CreateRelationship(uri, Opc.TargetMode.Internal, ApplicationPackagePrefix + DependencyRelationType);

            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);

            using (Stream outStream = packagePart.GetStream()) {
                doc.Save(outStream);
            }

        }

        private void WriteReferences(Opc.Package package) {
            foreach (var referenceFile in References) {
                using (Stream readStream = referenceFile.Open()) {
                    var version = referenceFile.TargetFramework == null ? String.Empty : referenceFile.TargetFramework.ToString();
                    string path = Path.Combine("Reference", version, referenceFile.Name);
                    Uri uri = UriHelper.CreatePartUri(path);
                    Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);
                    using (Stream outStream = packagePart.GetStream()) {
                        readStream.CopyTo(outStream);
                    }
                    package.CreateRelationship(uri, Opc.TargetMode.Internal, ApplicationPackagePrefix + ReferenceRelationType);
                }
            }
        }

        private void WriteFiles(Opc.Package package) {
            foreach (var fileTypeValue in Enum.GetValues(typeof(PackageFileType))) {
                var fileType = (PackageFileType)fileTypeValue;
                foreach (var packageFile in _packageFiles[fileType]) {
                    using (Stream stream = packageFile.Open()){
                        string destinationPath = ResolvePackageInternalDestinationPath(packageFile.Path, fileType);
                        CreatePart(package, fileType, packageFile.Path, stream);
                    }
                }
            }
        }

        private void CreatePart(Opc.Package package, PackageFileType fileType, string packagePath, Stream sourceStream) {
            Uri uri = UriHelper.CreatePartUri(packagePath);
            
            // Create the relationship type
            package.CreateRelationship(uri, Opc.TargetMode.Internal, GetPackageRelationName(fileType));
            
            // Create the part
            Opc.PackagePart packagePart = package.CreatePart(uri, DefaultContentType);
    
            using (Stream outStream = packagePart.GetStream()) {
                sourceStream.CopyTo(outStream);
            }
        }

        private static string ResolvePackageInternalDestinationPath(string path, PackageFileType fileType) {
            switch (fileType) {
                case PackageFileType.Content:
                    return Path.Combine(fileType.ToString(), path);
                default:
                    return path;
            }
        }

        private static string GetPackageRelationName(PackageFileType fileType) {
            return ApplicationPackagePrefix + fileType.ToString();
        }
    }
}

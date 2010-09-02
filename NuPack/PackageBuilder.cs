using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Internal.Web.Utils;
using OPC = System.IO.Packaging;
using System.Diagnostics;

namespace NuPack {
    public class PackageBuilder {
        // NOTE: Follow up on Bug 927501 prior to shipping. Need to ensure this is a value we can ship.
        internal const string ApplicationPackagePrefix = "http://schemas.microsoft.com/net/package/2010/";
        internal const string ConfigurationRelationshipType = ApplicationPackagePrefix + "configuration";
        internal const string DependenciesRelationshipType = ApplicationPackagePrefix + "dependencies";
        internal const string ReferencesRelationshipType = ApplicationPackagePrefix + "reference";
        internal const string ContentRelationshipType = ApplicationPackagePrefix + "content";
        internal const string ToolRelationshipType = ApplicationPackagePrefix + "tool";
        internal const string DependenciesFileName = "dependencies.xml";

        private static readonly Dictionary<string, string> KnownRelationships = new Dictionary<string, string> { 
                { "Content", ContentRelationshipType }, 
                { "References", ReferencesRelationshipType },
                { "Tools" , ToolRelationshipType }
        };


        internal static void ApplyManifest(OPC.Package package, string manifestFilePath) {
            if (package == null) {
                throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "package");
            }
            if (manifestFilePath == null) {
                throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "manifestFilePath");
            }

            using (Stream stream = File.OpenRead(manifestFilePath)) {
                ApplyManifest(package, stream);
            }
        }

        internal static void ApplyManifest(OPC.Package package, Stream manifestStream) {
            XDocument document = XDocument.Load(manifestStream);
            SetPropertiesFromManifest(package.PackageProperties, document);
            AddConfigAndDependenciesFromManifest(package, document);
        }

        private static void SetPropertiesFromManifest(PackageProperties packageProperties, XDocument manifestFile) {
            XElement root = manifestFile.Root;

            if (root.Attribute("Id") != null) {
                packageProperties.Identifier = root.Attribute("Id").Value;
            }
            if (root.Element("Version") != null) {
                packageProperties.Version = root.Element("Version").Value;
            }

            packageProperties.Title = manifestFile.Root.GetOptionalElementValue("Name");
            packageProperties.Description = manifestFile.Root.GetOptionalElementValue("Description");
            packageProperties.Creator = manifestFile.Root.GetOptionalElementValue("Author");
            packageProperties.ContentType = manifestFile.Root.GetOptionalElementValue("ContentType");
        }

        private static void AddConfigAndDependenciesFromManifest(OPC.Package package, XDocument manifestFile) {
            var dependenciesElement = manifestFile.Root.Element("Dependencies");
            if (dependenciesElement != null) {
                CreatePart(package, DependenciesRelationshipType, DependenciesFileName, dependenciesElement);
            }

            var configurationElement = manifestFile.Root.Element("configuration");
            if (configurationElement != null) {
                CreatePart(package, ConfigurationRelationshipType, "web.config", configurationElement);
            }
        }

        /// <summary>
        /// Adds content to the package using the folder hierarchy to determine the type of relation to associate the file with.
        /// Files under /content are stored with the type ContentRelationship, under /references are stored with the type ReferenceRelationship
        /// and under /tools are stored with the type ToolsRelationship
        /// Files that do not follow this directory structure are ignored.
        /// </summary>
        /// <param name="package">Package instance to add content to</param>
        /// <param name="contentPath">The path to the resource to be added.
        /// Paths can be complete path to a file (e.g. directory/filename.ext) , a directory (e.g. directory) 
        /// or a path with wildcard (e.g. *.css, directory/*.css)
        /// </param>
        internal static void AddPackageContent(OPC.Package package, string contentPath) {
            if (package == null) {
                throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "package");
            }
            if (contentPath == null) {
                throw new ArgumentNullException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "contentPath");
            }

            string searchDirectory = null;
            string searchFilter = GetSearchFilter(contentPath, out searchDirectory);

            var files = Directory.GetFiles(searchDirectory, searchFilter, SearchOption.AllDirectories);
            foreach (var filePath in files) {
                AddFileToPackage(package, filePath);
            }
        }

        internal static void AddFileToPackage(OPC.Package package, string filePath) {
            using (Stream stream = File.OpenRead(filePath)) {
                AddFileToPackage(package, filePath, stream);
            }
        }

        internal static void AddFileToPackage(OPC.Package package, string filePath, Stream fileStream) {
            // The directory structure is used to identify the type of relationship
            // <root>/Content/<foo> becomes a content relation. Attempt to find the root portion of it and then 
            // find the first folder that occurs after it.
            Debug.Assert(!String.IsNullOrEmpty(filePath));
            //Remove leading slashes
            filePath = filePath.TrimStart('\\');
            if (!filePath.Contains(Path.DirectorySeparatorChar)) {
                //The path contains a single file. Since no directory structure can be constructed, return
                return;
            }

            string pathRoot = filePath.Split(Path.DirectorySeparatorChar).FirstOrDefault();
            string relativePath = pathRoot.Any() ? filePath.Substring(pathRoot.Length + 1) : filePath;
            var comparison = StringComparison.InvariantCultureIgnoreCase;
            var folder = KnownRelationships.Keys.FirstOrDefault(key => relativePath.StartsWith(key, comparison));
            if (folder != null) {
                // Only the path relative to <folder>/ is recreated in the package. Identify path relative to the <folder>.
                int packagePathIndex = relativePath.IndexOf(folder + Path.DirectorySeparatorChar, comparison);
                string packagePath = relativePath.Substring(packagePathIndex);

                CreatePart(package, KnownRelationships[folder], packagePath, fileStream);
            }
        }

        internal static string GetSearchFilter(string contentPath, out string searchDirectory) {
            // Start by assuming that the content path is a directory without wildchar searches.
            // In this case we would add use consider all the files in its sub-folders
            string searchFilter = "*";
            searchDirectory = contentPath;

            if (contentPath.Contains('*')) {
                // The content path contains wildchar. This could be of the format *.* or <some path>/*.*
                // For the latter case, identify the folder
                searchDirectory = Path.GetDirectoryName(searchDirectory);

                if (String.IsNullOrEmpty(searchDirectory)) {
                    // For the first case (*.*), the searchDirectory would be empty. Set it to the working directory.
                    // The entire content path becomes the filter
                    searchFilter = contentPath;
                    searchDirectory = ".";
                }
                else if (contentPath.Length > searchDirectory.Length) {
                    // If the search is of the format <some folder>/*, extract the part after the slash
                    searchFilter = contentPath.Substring(searchDirectory.Length + 1);
                }
            }
            return searchFilter;
        }

        internal static void CreatePart(OPC.Package package, string relationshipType, string path, Stream contentStream) {
            var part = CreatePartOnPackage(package, relationshipType, path);
            // Copy the contents to the package part
            using (Stream partStream = part.GetStream()) {
                contentStream.CopyTo(partStream);
            }
        }

        internal static void CreatePart(OPC.Package package, string relationshipType, string path, XElement element) {
            var part = CreatePartOnPackage(package, relationshipType, path);
            // Copy the contents to the package part
            using (Stream partStream = part.GetStream()) {
                element.Save(partStream);
            }
        }

        private static OPC.PackagePart CreatePartOnPackage(OPC.Package package, string relationshipType, string path) {
             Uri uri = CreatePartUri(path);
            // Create the relationship type
             package.CreateRelationship(uri, OPC.TargetMode.Internal, relationshipType);
            // Create the part
            return package.CreatePart(uri, MimeMapping.GetMimeMapping(path));
        }

        private static Uri CreatePartUri(string path) {
            return OPC.PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
        }
    }
}

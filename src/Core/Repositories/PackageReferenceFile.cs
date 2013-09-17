using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml;
using System.Xml.Linq;
using NuGet.Resources;

namespace NuGet
{
    public class PackageReferenceFile
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _constraints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _developmentFlags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public PackageReferenceFile(string path) :
            this(new PhysicalFileSystem(Path.GetDirectoryName(path)),
                                        Path.GetFileName(path))
        {
        }

        public PackageReferenceFile(IFileSystem fileSystem, string path)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "path");
            }

            FileSystem = fileSystem;
            _path = path;
        }

        private IFileSystem FileSystem { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        public IEnumerable<PackageReference> GetPackageReferences()
        {
            return GetPackageReferences(requireVersion: true);
        }

        public IEnumerable<PackageReference> GetPackageReferences(bool requireVersion)
        {
            XDocument document = GetDocument();

            if (document == null)
            {
                yield break;
            }

            foreach (var e in document.Root.Elements("package"))
            {
                string id = e.GetOptionalAttributeValue("id");
                string versionString = e.GetOptionalAttributeValue("version");
                string versionConstraintString = e.GetOptionalAttributeValue("allowedVersions");
                string targetFrameworkString = e.GetOptionalAttributeValue("targetFramework");
                string developmentFlagString = e.GetOptionalAttributeValue("developmentDependency");
                string requireReinstallationString = e.GetOptionalAttributeValue("requireReinstallation");
                SemanticVersion version = null;

                if (String.IsNullOrEmpty(id))
                {
                    // If the id is empty, ignore the record unless unspecified versions are allowed
                    continue;
                }

                if (String.IsNullOrEmpty(versionString))
                {
                    // If the version is empty, ignore the record unless unspecified versions are allowed
                    if (requireVersion)
                    {
                        continue;
                    }
                }
                else if (!SemanticVersion.TryParse(versionString, out version))
                {
                    throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.ReferenceFile_InvalidVersion, versionString, _path));
                }

                IVersionSpec versionConstaint = null;
                if (!String.IsNullOrEmpty(versionConstraintString))
                {
                    if (!VersionUtility.TryParseVersionSpec(versionConstraintString, out versionConstaint))
                    {
                        throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.ReferenceFile_InvalidVersion, versionConstraintString, _path));
                    }

                    _constraints[id] = versionConstraintString;
                }

                FrameworkName targetFramework = null;
                if (!String.IsNullOrEmpty(targetFrameworkString))
                {
                    targetFramework = VersionUtility.ParseFrameworkName(targetFrameworkString);
                    if (targetFramework == VersionUtility.UnsupportedFrameworkName)
                    {
                        targetFramework = null;
                    }
                }

                var developmentFlag = false;
                if (!String.IsNullOrEmpty(developmentFlagString))
                {
                    if (!Boolean.TryParse(developmentFlagString, out developmentFlag))
                    {
                        throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.ReferenceFile_InvalidDevelopmentFlag, developmentFlagString, _path));
                    }

                    _developmentFlags[id] = developmentFlagString;
                }

                var requireReinstallation = false;
                if (!String.IsNullOrEmpty(requireReinstallationString))
                {
                    if (!Boolean.TryParse(requireReinstallationString, out requireReinstallation))
                    {
                        throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.ReferenceFile_InvalidRequireReinstallationFlag, requireReinstallationString, _path));
                    }
                }

                yield return new PackageReference(id, version, versionConstaint, targetFramework, developmentFlag, requireReinstallation);
            }
        }

        /// <summary>
        /// Deletes an entry from the file with matching id and version. Returns true if the file was deleted.
        /// </summary>
        public bool DeleteEntry(string id, SemanticVersion version)
        {
            XDocument document = GetDocument();

            if (document == null)
            {
                return false;
            }

            return DeleteEntry(document, id, version);
        }

        public bool EntryExists(string packageId, SemanticVersion version)
        {
            XDocument document = GetDocument();
            if (document == null)
            {
                return false;
            }

            return FindEntry(document, packageId, version) != null;
        }

        public void AddEntry(string id, SemanticVersion version)
        {
            AddEntry(id, version, targetFramework: null);
        }

        public void AddEntry(string id, SemanticVersion version, FrameworkName targetFramework)
        {
            XDocument document = GetDocument(createIfNotExists: true);

            AddEntry(document, id, version, targetFramework);
        }

        public void MarkEntryForReinstallation(string id, SemanticVersion version, FrameworkName targetFramework, bool requireReinstallation)
        {
            Debug.Assert(id != null);
            Debug.Assert(version != null);

            XDocument document = GetDocument();
            if (document != null)
            {
                DeleteEntry(id, version);
                AddEntry(document, id, version, targetFramework, requireReinstallation);
            }
        }

        private void AddEntry(XDocument document, string id, SemanticVersion version, FrameworkName targetFramework)
        {
            AddEntry(document, id, version, targetFramework, requireReinstallation: false);
        }

        private void AddEntry(XDocument document, string id, SemanticVersion version, FrameworkName targetFramework, bool requireReinstallation)
        {
            XElement element = FindEntry(document, id, version);

            if (element != null)
            {
                element.Remove();
            }

            var newElement = new XElement("package",
                                  new XAttribute("id", id),
                                  new XAttribute("version", version));
            if (targetFramework != null)
            {
                newElement.Add(new XAttribute("targetFramework", VersionUtility.GetShortFrameworkName(targetFramework)));
            }

            // Restore the version constraint
            string versionConstraint;
            if (_constraints.TryGetValue(id, out versionConstraint))
            {
                newElement.Add(new XAttribute("allowedVersions", versionConstraint));
            }

            // Restore the development dependency flag
            string developmentFlag;
            if (_developmentFlags.TryGetValue(id, out developmentFlag))
            {
                newElement.Add(new XAttribute("developmentDependency", developmentFlag));
            }

            if (requireReinstallation)
            {
                newElement.Add(new XAttribute("requireReinstallation", Boolean.TrueString));
            }

            document.Root.Add(newElement);

            SaveDocument(document);
        }

        private static XElement FindEntry(XDocument document, string id, SemanticVersion version)
        {
            if (String.IsNullOrEmpty(id))
            {
                return null;
            }

            return (from e in document.Root.Elements("package")
                    let entryId = e.GetOptionalAttributeValue("id")
                    let entryVersion = SemanticVersion.ParseOptionalVersion(e.GetOptionalAttributeValue("version"))
                    where entryId != null && entryVersion != null
                    where id.Equals(entryId, StringComparison.OrdinalIgnoreCase) && (version == null || entryVersion.Equals(version))
                    select e).FirstOrDefault();
        }

        private void SaveDocument(XDocument document)
        {
            // Sort the elements by package id and only take valid entries (one with both id and version)
            var packageElements = (from e in document.Root.Elements("package")
                                   let id = e.GetOptionalAttributeValue("id")
                                   let version = e.GetOptionalAttributeValue("version")
                                   where !String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(version)
                                   orderby id
                                   select e).ToList();

            // Remove all elements
            document.Root.RemoveAll();

            // Re-add them sorted
            document.Root.Add(packageElements);

            FileSystem.AddFile(_path, document.Save);
        }

        private bool DeleteEntry(XDocument document, string id, SemanticVersion version)
        {
            XElement element = FindEntry(document, id, version);

            if (element != null)
            {
                // Preserve the allowedVersions attribute for this package id (if any defined)
                var versionConstraint = element.GetOptionalAttributeValue("allowedVersions");

                if (!String.IsNullOrEmpty(versionConstraint))
                {
                    _constraints[id] = versionConstraint;
                }

                // Preserve the developmentDependency attribute for this package id (if any defined)
                var developmentFlag = element.GetOptionalAttributeValue("developmentDependency");
                if (!String.IsNullOrEmpty(developmentFlag))
                {
                    _developmentFlags[id] = developmentFlag;
                }

                // Remove the element from the xml dom
                element.Remove();

                // Always try and save the document, this works around a source control issue for solution-level packages.config.
                SaveDocument(document);

                if (!document.Root.HasElements)
                {
                    // Remove the file if there are no more elements
                    FileSystem.DeleteFile(_path);

                    return true;
                }
            }

            return false;
        }

        private XDocument GetDocument(bool createIfNotExists = false)
        {
            try
            {
                // If the file exists then open and return it
                if (FileSystem.FileExists(_path))
                {
                    using (Stream stream = FileSystem.OpenFile(_path))
                    {
                        return XmlUtility.LoadSafe(stream);
                    }
                }

                // If it doesn't exist and we're creating a new file then return a
                // document with an empty packages node
                if (createIfNotExists)
                {
                    return new XDocument(new XElement("packages"));
                }

                return null;
            }
            catch (XmlException e)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.ErrorReadingFile, FileSystem.GetFullPath(_path)), e);
            }
        }
    }
}

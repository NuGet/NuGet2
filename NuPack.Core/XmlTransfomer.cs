using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuPack {
    internal class XmlTransfomer : IPackageFileModifier {
        public void Modify(IPackageFile file, ProjectSystem projectSystem) {
            string targetPath = Path.GetFileNameWithoutExtension(file.Path);

            // Get the xml fragment
            XElement xmlFragment = GetXml(file);

            XDocument transformDocument = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Do a merge
            transformDocument.Root.MergeWith(xmlFragment);

            // Save the new file
            projectSystem.AddFile(targetPath, transformDocument.Save);
        }

        public void Revert(IPackageFile file, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem) {
            string targetPath = Path.GetFileNameWithoutExtension(file.Path);

            // Get the xml snippet
            XElement xmlFragment = GetXml(file);

            XDocument document = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);
            
            // Merge the other xml elements into one element within this xml hierarchy (matching the config file path)
            var mergedFragments = matchingFiles.Select(GetXml)
                                               .Aggregate(new XElement(xmlFragment.Name), (left, right) => left.MergeWith(right));

            // Take the difference of the xml and remove it from the main xml file
            document.Root.Except(xmlFragment.Except(mergedFragments));

            projectSystem.AddFile(targetPath, document.Save);
        }

        private static XElement GetXml(IPackageFile file) {
            using (Stream stream = file.Open()) {
                return XElement.Load(stream);
            }
        }
    }
}
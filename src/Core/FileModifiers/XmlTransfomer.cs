using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuGet {
    internal class XmlTransfomer : IPackageFileTransformer {
        private readonly IDictionary<XName, Action<XElement, XElement>> _nodeActions;
        
        public XmlTransfomer(IDictionary<XName, Action<XElement, XElement>> nodeActions) {
            _nodeActions = nodeActions;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We are creating a new stream for the caller to use")]
        public void TransformFile(IPackageFile file, string targetPath, IProjectSystem projectSystem) {
            // Get the xml fragment
            XElement xmlFragment = GetXml(file, projectSystem);

            XDocument transformDocument = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Do a merge
            transformDocument.Root.MergeWith(xmlFragment, _nodeActions);

            projectSystem.AddFile(targetPath, transformDocument.Save);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We are creating a new stream for the caller to use")]
        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, IProjectSystem projectSystem) {
            // Get the xml snippet
            XElement xmlFragment = GetXml(file, projectSystem);

            XDocument document = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Merge the other xml elements into one element within this xml hierarchy (matching the config file path)
            var mergedFragments = matchingFiles.Select(f => GetXml(f, projectSystem))
                                               .Aggregate(new XElement(xmlFragment.Name), (left, right) => left.MergeWith(right, _nodeActions));

            // Take the difference of the xml and remove it from the main xml file
            document.Root.Except(xmlFragment.Except(mergedFragments));

            // Save the new content to the file system
            projectSystem.AddFile(targetPath, document.Save);
        }

        private static XElement GetXml(IPackageFile file, IProjectSystem projectSystem) {
            using (Stream stream = file.GetStream()) {
                var content = Preprocessor.Process(file, projectSystem);
                return XElement.Parse(content);
            }
        }
    }
}

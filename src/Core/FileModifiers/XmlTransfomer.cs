using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;
using NuGet.Diagnostics;

namespace NuGet
{
    internal class XmlTransformer : IPackageFileTransformer
    {
        private readonly IDictionary<XName, Action<XElement, XElement>> _nodeActions;

        public XmlTransformer(IDictionary<XName, Action<XElement, XElement>> nodeActions)
        {
            _nodeActions = nodeActions;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We are creating a new stream for the caller to use")]
        public virtual void TransformFile(IPackageFile file, string targetPath, IProjectSystem projectSystem)
        {
            NuGetTraceSources.XmlTransformer.Info(
                "applyingxmltransform",
                "[{0}] Applying XML transform to {1}",
                projectSystem.ProjectName,
                targetPath);
            
            // Get the xml fragment
            XElement xmlFragment = GetXml(file, projectSystem);

            XDocument transformDocument = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Do a merge
            transformDocument.Root.MergeWith(xmlFragment, _nodeActions);

            projectSystem.AddFile(targetPath, transformDocument.Save);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We are creating a new stream for the caller to use")]
        public virtual void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, IProjectSystem projectSystem)
        {
            NuGetTraceSources.XmlTransformer.Info(
                "revertingxmltransform",
                "[{0}] Reverting XML transform of {1}",
                projectSystem.ProjectName,
                targetPath);

            // Get the xml snippet
            XElement xmlFragment = GetXml(file, projectSystem);

            XDocument document = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Merge the other xml elements into one element within this xml hierarchy (matching the config file path)
            var mergedFragments = matchingFiles.Select(f => GetXml(f, projectSystem))
                                               .Aggregate(new XElement(xmlFragment.Name), (left, right) => left.MergeWith(right, _nodeActions));

            // Take the difference of the xml and remove it from the main xml file
            document.Root.Except(xmlFragment.Except(mergedFragments));

            // Save the new content to the file system
            using (var fileStream = projectSystem.CreateFile(targetPath))
            {
                document.Save(fileStream);
            }
        }

        private static XElement GetXml(IPackageFile file, IProjectSystem projectSystem)
        {
            var content = Preprocessor.Process(file, projectSystem);
            return XElement.Parse(content, LoadOptions.PreserveWhitespace);
        }
    }
}

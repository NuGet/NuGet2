using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuGet.VisualStudio;

namespace NuGet.Dialog
{
    public class Visualizer
    {
        private const string dgmlNS = "http://schemas.microsoft.com/vs/2009/dgml";
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly ISolutionManager _solutionManager;

        public Visualizer(IVsPackageManagerFactory packageManagerFactory, ISolutionManager solutionManager)
        {
            _packageManagerFactory = packageManagerFactory;
            _solutionManager = solutionManager;
        }

        public string CreateGraph()
        {
            // We only use the package manager to locate the LocalRepository, we should be fine disabling fallback.
            var packageManager = _packageManagerFactory.CreatePackageManager(ServiceLocator.GetInstance<IPackageRepository>(), useFallbackForDependencies: false);
            var solutionManager = new SolutionManager();

            var nodes = new List<DGMLNode>();
            var links = new List<DGMLLink>();
            VisitProjects(packageManager, solutionManager, nodes, links);

            return GenerateDGML(nodes, links);
        }

        private static void VisitProjects(IVsPackageManager packageManager, SolutionManager solutionManager, List<DGMLNode> nodes, List<DGMLLink> links)
        {
            foreach (var project in solutionManager.GetProjects())
            {
                var projectManager = packageManager.GetProjectManager(project);
                var repo = projectManager.LocalRepository;
                if (!repo.GetPackages().Any())
                {
                    // Project has no packages. Ignore it.
                    continue;
                }
                // Project has packages. Add a node for it
                nodes.Add(new DGMLNode { Name = project.GetCustomUniqueName(), Label = project.GetDisplayName(), Category = Resources.Visualizer_Project });

                var dependencies = VisitProjectPackages(nodes, links, repo);
                var installedPackages = repo.GetPackages().Except(dependencies);
                links.AddRange(installedPackages.Select(c => new DGMLLink { SourceName = project.GetCustomUniqueName(), DestName = c.GetFullName(), Category = Resources.Visualizer_InstalledPackage }));
            }
        }

        private static IEnumerable<IPackage> VisitProjectPackages(List<DGMLNode> nodes, List<DGMLLink> links, IPackageRepository repo)
        {
            var mapping = repo.GetPackages().ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
            var dependencies = new HashSet<IPackage>();
            foreach (var package in repo.GetPackages())
            {
                var packageName = package.GetFullName();
                nodes.Add(new DGMLNode { Name = packageName, Label = packageName, Category = Resources.Visualizer_Package });

                foreach (var dependency in package.GetCompatiblePackageDependencies(targetFramework: null))
                {
                    IPackage dependentPackage;
                    if (mapping.TryGetValue(dependency.Id, out dependentPackage))
                    {
                        dependencies.Add(dependentPackage);
                        links.Add(new DGMLLink { SourceName = packageName, DestName = dependentPackage.GetFullName(), Category = Resources.Visualizer_PackageDependency });
                    }
                }
            }
            return dependencies;
        }

        private string GenerateDGML(List<DGMLNode> nodes, List<DGMLLink> links)
        {
            bool hasDependencies = links.Any(l => l.Category == Resources.Visualizer_PackageDependency);
            var document = new XDocument(
                new XElement(XName.Get("DirectedGraph", dgmlNS),
                    new XAttribute("GraphDirection", "LeftToRight"),
                    new XElement(XName.Get("Nodes", dgmlNS),
                        from item in nodes select new XElement(XName.Get("Node", dgmlNS), new XAttribute("Id", item.Name), new XAttribute("Label", item.Label), new XAttribute("Category", item.Category))),
                    new XElement(XName.Get("Links", dgmlNS),
                        from item in links
                        select new XElement(XName.Get("Link", dgmlNS), new XAttribute("Source", item.SourceName), new XAttribute("Target", item.DestName),
                            new XAttribute("Category", item.Category))),
                    new XElement(XName.Get("Categories", dgmlNS),
                        new XElement(XName.Get("Category", dgmlNS), new XAttribute("Id", Resources.Visualizer_Project)),
                        new XElement(XName.Get("Category", dgmlNS), new XAttribute("Id", Resources.Visualizer_Package))),
                    new XElement(XName.Get("Styles", dgmlNS),
                        StyleElement(Resources.Visualizer_Project, "Node", "Background", "Blue"),
                        hasDependencies ? StyleElement(Resources.Visualizer_PackageDependency, "Link", "Background", "Yellow") : null))
            );
            var saveFilePath = Path.Combine(_solutionManager.SolutionDirectory, "Packages.dgml");
            document.Save(saveFilePath);
            return saveFilePath;
        }

        private static XElement StyleElement(string category, string targetType, string propertyName, string propertyValue)
        {
            return new XElement(XName.Get("Style", dgmlNS), new XAttribute("TargetType", targetType), new XAttribute("GroupLabel", category), new XAttribute("ValueLabel", "True"),
                    new XElement(XName.Get("Condition", dgmlNS), new XAttribute("Expression", String.Format(CultureInfo.InvariantCulture, "HasCategory('{0}')", category))),
                    new XElement(XName.Get("Setter", dgmlNS), new XAttribute("Property", propertyName), new XAttribute("Value", propertyValue)));
        }

        private class DGMLNode : IEquatable<DGMLNode>
        {
            public string Name { get; set; }

            public string Label { get; set; }

            public string Category { get; set; }

            public bool Equals(DGMLNode other)
            {
                return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class DGMLLink
        {
            public string SourceName { get; set; }

            public string DestName { get; set; }

            public string Category { get; set; }
        }
    }
}
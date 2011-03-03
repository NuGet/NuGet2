using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Internal.Web.Utils;

namespace NuGet.Runtime {
    /// <summary>
    /// Class that manages the binding redirect config section
    /// </summary>
    public class BindingRedirectManager {
        private static readonly XName AssemblyBindingName = AssemblyBinding.GetQualifiedName("assemblyBinding");
        private static readonly XName DependentAssemblyName = AssemblyBinding.GetQualifiedName("dependentAssembly");

        private readonly IFileSystem _fileSystem;
        private readonly string _configurationPath;

        public BindingRedirectManager(IFileSystem fileSystem, string configurationPath) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            if (String.IsNullOrEmpty(configurationPath)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "configurationPath");
            }
            _fileSystem = fileSystem;
            _configurationPath = configurationPath;
        }

        public void AddBindingRedirects(IEnumerable<AssemblyBinding> bindingRedirects) {
            if (bindingRedirects == null) {
                throw new ArgumentNullException("bindingRedirects");
            }

            // Do nothing if there are no binding redirects to add, bail out
            if (!bindingRedirects.Any()) {
                return;
            }

            // Get the configuration file
            XDocument document = GetConfiguration();

            // Get the runtime element
            XElement runtime = document.Root.Element("runtime");

            if (runtime == null) {
                // Add the runtime element to the configuration document
                runtime = new XElement("runtime");
                document.Root.Add(runtime);
            }

            // Get all of the current bindings in config
            ILookup<AssemblyBinding, XElement> currentBindings = GetAssemblyBindings(document);

            XElement assemblyBindingElement = null;
            foreach (var bindingRedirect in bindingRedirects) {
                // Look to see if we already have this in the list of bindings already in config.
                if (currentBindings.Contains(bindingRedirect)) {                    
                    // Remove the assembly binding elements
                    foreach (var bindingElement in currentBindings[bindingRedirect]) {
                        RemoveElement(bindingElement);
                    }
                }

                if (assemblyBindingElement == null) {
                    // Get an assembly binding element to use
                    assemblyBindingElement = GetAssemblyBindingElement(runtime);
                }
                // Add the binding to that element
                assemblyBindingElement.Add(bindingRedirect.ToXElement());
            }

            // Save the file
            Save(document);
        }

        public void RemoveBindingRedirects(IEnumerable<AssemblyBinding> bindingRedirects) {
            if (bindingRedirects == null) {
                throw new ArgumentNullException("bindingRedirects");
            }

            // Do nothing if there are no binding redirects to remove, bail out
            if (!bindingRedirects.Any()) {
                return;
            }

            // Get the configuration file
            XDocument document = GetConfiguration();

            // Get all of the current bindings in config
            ILookup<AssemblyBinding, XElement> currentBindings = GetAssemblyBindings(document);

            if (!currentBindings.Any()) {
                return;
            }

            foreach (var bindingRedirect in bindingRedirects) {
                if (currentBindings.Contains(bindingRedirect)) {
                    foreach (var bindingElement in currentBindings[bindingRedirect]) {
                        RemoveElement(bindingElement);
                    }
                }
            }

            // Save the file
            Save(document);
        }

        private static void RemoveElement(XElement element) {
            // Hold onto the parent element before removing the element
            XElement parentElement = element.Parent;

            // Remove the element from the document if we find a match
            element.Remove();

            if (!parentElement.HasElements) {
                parentElement.Remove();
            }
        }

        private static XElement GetAssemblyBindingElement(XElement runtime) {
            // Pick the first assembly binding element or create one if there aren't any
            XElement assemblyBinding = runtime.Elements(AssemblyBindingName).FirstOrDefault();
            if (assemblyBinding != null) {
                return assemblyBinding;
            }

            assemblyBinding = new XElement(AssemblyBindingName);
            runtime.Add(assemblyBinding);

            return assemblyBinding;
        }

        private void Save(XDocument document) {
            _fileSystem.AddFile(_configurationPath, document.Save);
        }

        private static ILookup<AssemblyBinding, XElement> GetAssemblyBindings(XDocument document) {
            XElement runtime = document.Root.Element("runtime");

            IEnumerable<XElement> assemblyBindingElements = Enumerable.Empty<XElement>();
            if (runtime != null) {
                assemblyBindingElements = GetAssemblyBindingElements(runtime);
            }

            // We're going to need to know which element is associated with what binding for removal
            var assemblyElementPairs = from dependentAssemblyElement in assemblyBindingElements
                                       select new {
                                           Binding = AssemblyBinding.Parse(dependentAssemblyElement),
                                           Element = dependentAssemblyElement
                                       };

            // Return a mapping from binding to element
            return assemblyElementPairs.ToLookup(p => p.Binding, p => p.Element);
        }

        private static IEnumerable<XElement> GetAssemblyBindingElements(XElement runtime) {
            return runtime.Elements(AssemblyBindingName)
                          .Elements(DependentAssemblyName);
        }

        private XDocument GetConfiguration() {
            return XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _configurationPath);
        }
    }
}

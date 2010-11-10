using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NuGet.Runtime {
    /// <summary>
    /// Class that manages the binding redirect config section
    /// </summary>
    public class BindingRedirectManager {
        private readonly IFileSystem _fileSystem;
        private readonly string _configurationPath;

        public BindingRedirectManager(IFileSystem fileSystem, string configurationPath) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            if (configurationPath == null) {
                throw new ArgumentNullException("configurationPath");
            }
            _fileSystem = fileSystem;
            _configurationPath = configurationPath;
        }

        /// <summary>
        /// Adds the specified binding redirect to the list.
        /// </summary>
        public void AddBindingRedirect(BindingRedirect bindingRedirect) {
            if (bindingRedirect == null) {
                throw new ArgumentNullException("bindingRedirect");
            }
            IDictionary<BindingRedirect, BindingRedirect> bindingRedirects = GetBindingRedirects();
            bindingRedirects[bindingRedirect] = bindingRedirect;
            PersistBindingRedirects(bindingRedirects.Values);
        }

        public void AddBindingRedirects(IEnumerable<BindingRedirect> bindingRedirects) {
            if (bindingRedirects == null) {
                throw new ArgumentNullException("bindingRedirects");
            }

            // Do nothing if there are no binding redirects to add
            if (!bindingRedirects.Any()) {
                return;
            }

            IDictionary<BindingRedirect, BindingRedirect> currentBindingRedirects = GetBindingRedirects();
            foreach (var bindingRedirect in bindingRedirects) {
                currentBindingRedirects[bindingRedirect] = bindingRedirect;
            }
            PersistBindingRedirects(currentBindingRedirects.Values);
        }

        /// <summary>
        /// Removes the specified binding redirect to the list.
        /// </summary>
        public void RemoveBindingRedirect(BindingRedirect bindingRedirect) {
            if (bindingRedirect == null) {
                throw new ArgumentNullException("bindingRedirect");
            }
            IDictionary<BindingRedirect, BindingRedirect> bindingRedirects = GetBindingRedirects();
            bindingRedirects.Remove(bindingRedirect);
            PersistBindingRedirects(bindingRedirects.Values);
        }

        private void PersistBindingRedirects(IEnumerable<BindingRedirect> bindings) {
            // Get the configuration file
            XDocument document = GetConfiguration();

            // Get the runtime element
            XElement runtime = document.Root.Element("runtime");

            if (runtime == null) {
                // We don't have any bindings so do nothing
                if (!bindings.Any()) {
                    return;
                }

                // Add the runtime element to the configuration document
                runtime = new XElement("runtime");
                document.Root.Add(runtime);
            }

            // Remove all bindings from the current element
            GetAssemblyBindingElements(runtime).ToList()
                                               .ForEach(e => e.Remove());

            // Add the new bindings
            foreach (var binding in bindings) {
                runtime.Add(binding.ToXElement());
            }

            // Remove the runtime element if there are no elements
            if (!runtime.HasElements) {
                runtime.Remove();
            }

            // Save the file
            Save(document);
        }

        private void Save(XDocument document) {
            _fileSystem.AddFile(_configurationPath, document.Save);
        }

        private IDictionary<BindingRedirect, BindingRedirect> GetBindingRedirects() {
            XDocument document = GetConfiguration();

            XElement runtime = document.Root.Element("runtime");

            if (runtime == null) {
                return new Dictionary<BindingRedirect, BindingRedirect>();
            }

            return GetAssemblyBindingElements(runtime).Select(BindingRedirect.Parse)
                                                      .ToDictionary(b => b);
        }

        private static IEnumerable<XElement> GetAssemblyBindingElements(XElement runtime) {
            return runtime.Elements(XName.Get("assemblyBinding", BindingRedirect.Namespace));
        }

        private XDocument GetConfiguration() {
            return XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _configurationPath);
        }
    }
}

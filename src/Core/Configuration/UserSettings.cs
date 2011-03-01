using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using Microsoft.Internal.Web.Utils;

namespace NuGet.Configuration {
    public class UserSettings: ISettings {
        private XDocument _config;
        private string _configLocation;
        private IFileSystem _fileSystem;

        public UserSettings(IFileSystem fileSystem) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
            _configLocation = Path.Combine(_fileSystem.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet", "Nuget.Config");
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _configLocation);
        }

        public string GetValue(string section, string key) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            var kvps = GetValues(section);
            if (kvps == null || !kvps.ContainsKey(key)) {
                return null;
            }
            return kvps[key];
        }

        public IDictionary<string, string> GetValues(string section) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            
            try {
                var sectionElement = _config.Root.Element(section);
                if (sectionElement == null) {
                    return null;
                }

                var kvps = new Dictionary<string, string>();
                foreach (var e in sectionElement.Elements("Add")) {
                    var key = e.GetOptionalAttributeValue("key");
                    var value = e.GetOptionalAttributeValue("value");
                    if (!String.IsNullOrEmpty(key) && value != null) {
                        kvps.Add(key, value);
                    }
                }

                return kvps;
            }
            catch (Exception e) {
                throw new Exception("There was an error with the file format.", e);
            }
        }

        public void SetValue(string section, string key, string value) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null) {
                sectionElement = new XElement(section);
                _config.Root.Add(sectionElement);
            }

            foreach (var e in sectionElement.Elements("Add")) {
                var tempKey = e.GetOptionalAttributeValue("key");
                var tempValue = e.GetOptionalAttributeValue("value");

                if (tempKey == key) {
                    e.SetAttributeValue("value", value);
                    Save(_config);
                    return;
                }
            }

            var addElement = new XElement("Add");
            addElement.SetAttributeValue("key", key);
            addElement.SetAttributeValue("value", value);
            sectionElement.Add(addElement);
            Save(_config);

        }

        private void Save(XDocument document) {
            _fileSystem.AddFile(_configLocation, document.Save);
        }
    }
}

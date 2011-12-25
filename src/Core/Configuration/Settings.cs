using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Internal.Web.Utils;
using NuGet.Resources;

namespace NuGet
{
    public class Settings : ISettings
    {
        private static Lazy<ISettings> _defaultSettings = new Lazy<ISettings>(CreateDefaultSettings);
        private readonly XDocument _config;
        private readonly IFileSystem _fileSystem;

        public Settings(IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, Constants.SettingsFileName);
        }

        public static ISettings DefaultSettings
        {
            get { return _defaultSettings.Value; }
        }

        public string ConfigFilePath
        {
            get { return Path.Combine(_fileSystem.Root, Constants.SettingsFileName); }
        }

        public string GetValue(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            // Get the section and return null if it doesn't exist
            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                return null;
            }

            // Get the add element that matches the key and return null if it doesn't exist
            var element = FindElementByKey(sectionElement, key);
            if (element == null)
            {
                return null;
            }

            // Return the optional value which if not there will be null;
            return element.GetOptionalAttributeValue("value");
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                return null;
            }
            return sectionElement.Elements("add")
                                 .Select(ReadValue)
                                 .ToList()
                                 .AsReadOnly();
        }

        public void SetValue(string section, string key, string value)
        {
            SetValueInternal(section, key, value);
            Save();
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            foreach (var kvp in values)
            {
                SetValueInternal(section, kvp.Key, kvp.Value);
            }
            Save();
        }

        private void SetValueInternal(string section, string key, string value)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                sectionElement = new XElement(section);
                _config.Root.Add(sectionElement);
            }

            var element = FindElementByKey(sectionElement, key);
            if (element != null)
            {
                element.SetAttributeValue("value", value);
                Save();
            }
            else
            {
                sectionElement.Add(new XElement("add", new XAttribute("key", key), new XAttribute("value", value)));
            }
        }

        public bool DeleteValue(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                return false;
            }

            var elementToDelete = FindElementByKey(sectionElement, key);
            if (elementToDelete == null)
            {
                return false;
            }
            elementToDelete.Remove();
            Save();
            return true;
        }

        public bool DeleteSection(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null)
            {
                return false;
            }

            sectionElement.Remove();
            Save();
            return true;
        }

        private void Save()
        {
            _fileSystem.AddFile(Constants.SettingsFileName, _config.Save);
        }

        private KeyValuePair<string, string> ReadValue(XElement element)
        {
            var keyAttribute = element.Attribute("key");
            var valueAttribute = element.Attribute("value");

            if (keyAttribute == null || String.IsNullOrEmpty(keyAttribute.Value) || valueAttribute == null)
            {
                throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.UserSettings_UnableToParseConfigFile, ConfigFilePath));
            }

            return new KeyValuePair<string, string>(keyAttribute.Value, valueAttribute.Value);
        }

        private static XElement FindElementByKey(XElement sectionElement, string key)
        {
            return sectionElement.Elements("add")
                                        .FirstOrDefault(s => key.Equals(s.GetOptionalAttributeValue("key"), StringComparison.OrdinalIgnoreCase));
        }

        private static ISettings CreateDefaultSettings()
        {
            IFileSystem fileSystem;
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (String.IsNullOrEmpty(appDataPath))
            {
                // If there is no AppData folder, use a null file system to make the Settings object do nothing
                return NullSettings.Instance;
            }
            else
            {
                string defaultSettingsPath = Path.Combine(appDataPath, "NuGet");
                fileSystem = new PhysicalFileSystem(defaultSettingsPath);
            }

            return new Settings(fileSystem);
        }
    }
}

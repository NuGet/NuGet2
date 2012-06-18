using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NuGet.Resources;

namespace NuGet
{
    public class Settings : ISettings
    {
        private readonly XDocument _config;
        private readonly IFileSystem _fileSystem;
        private readonly string _fileName;

        public Settings(IFileSystem fileSystem)
            :this(fileSystem, Constants.SettingsFileName)
        {
        }

        public Settings(IFileSystem fileSystem, string fileName)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (String.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "fileName");
            }
            _fileSystem = fileSystem;
            _fileName = fileName;
            _config = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _fileName);
        }

        public string ConfigFilePath
        {
            get { return Path.Combine(_fileSystem.Root, _fileName); }
        }

        public static ISettings LoadDefaultSettings(IFileSystem currentDir)
        {
            // Walk up the tree to find a workspace config file
            // if not found, attempt to load config file in user's application data
            var currentRoot = currentDir == null ? null : currentDir.Root;

            while (null != currentRoot)
            {
                var workspaceSettingsDir = Path.Combine(currentRoot, Constants.NuGetWorkspaceSettingsFolder);
                var workspaceSettingsFile = Path.Combine(workspaceSettingsDir, Constants.WorkspaceSettingsFileName);

                if (currentDir.FileExists(workspaceSettingsFile))
                {
                    try
                    {
                        return new Settings(new PhysicalFileSystem(workspaceSettingsDir), Constants.WorkspaceSettingsFileName);
                    }
                    catch (XmlException)
                    {
                        // Work Item 1531: If the config file is malformed and the constructor throws, NuGet fails to load in VS. 
                        // Returning a null instance prevents us from silently failing and also from picking up the wrong config
                        return NullSettings.Instance;
                    }
                }

                currentRoot = Path.GetDirectoryName(currentRoot);
            }


            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!String.IsNullOrEmpty(appDataPath))
            {
                var defaultSettingsPath = Path.Combine(appDataPath, "NuGet");
                var fileSystem = new PhysicalFileSystem(defaultSettingsPath);
                try
                {
                    return new Settings(fileSystem);
                }
                catch (XmlException)
                {
                    // Work Item 1531: If the config file is malformed and the constructor throws, NuGet fails to load in VS. 
                    // Returning a null instance prevents us from silently failing.
                }
            }

            // If there is no AppData folder, use a null file system to make the Settings object do nothing
            return NullSettings.Instance;
        }

        public string GetValue(string section, string key)
        {
            return GetValue(section, key, false);
        }

        public string GetValue(string section, string key, bool isPath)
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
            var sectionElement = GetSection(_config.Root, section);
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
            string ret = element.GetOptionalAttributeValue("value");
            if (!isPath || string.IsNullOrEmpty(ret))
            {
                return ret;
            }
            // if value represents a path and relative to this file path was specified, 
            // append location of file
            return (ret.StartsWith("$\\", StringComparison.OrdinalIgnoreCase) || ret.StartsWith("$/", StringComparison.OrdinalIgnoreCase))
                       ? Path.GetFullPath(Path.Combine(_fileSystem.Root, ret.Substring(2)))
                       : ret;
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return EmptyList();
            }

            return ReadSection(sectionElement);
        }

        public IList<KeyValuePair<string, string>> GetNestedValues(string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return EmptyList();
            }
            var subSection = GetSection(sectionElement, key);
            if (subSection == null)
            {
                return EmptyList();
            }
            return ReadSection(subSection);
        }

        public void SetValue(string section, string key, string value)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            var sectionElement = GetOrCreateSection(_config.Root, section);
            SetValueInternal(sectionElement, key, value);
            Save();
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var sectionElement = GetOrCreateSection(_config.Root, section);
            foreach (var kvp in values)
            {
                SetValueInternal(sectionElement, kvp.Key, kvp.Value);
            }
            Save();
        }

        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var sectionElement = GetOrCreateSection(_config.Root, section);
            var element = GetOrCreateSection(sectionElement, key);

            foreach (var kvp in values)
            {
                SetValueInternal(element, kvp.Key, kvp.Value);
            }
            Save();
        }

        private void SetValueInternal(XElement sectionElement, string key, string value)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var element = FindElementByKey(sectionElement, key);
            if (element != null)
            {
                element.SetAttributeValue("value", value);
                Save();
            }
            else
            {
                sectionElement.Add(new XElement("add", 
                                                    new XAttribute("key", key), 
                                                    new XAttribute("value", value)));
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

            var sectionElement = GetSection(_config.Root, section);
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

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return false;
            }

            sectionElement.Remove();
            Save();
            return true;
        }
        
        protected IList<KeyValuePair<string, string>> ReadSection(XElement sectionElement)
        {
            var elements = sectionElement.Elements();
            var values = new List<KeyValuePair<string, string>>();
            foreach (var element in elements)
            {
                string elementName = element.Name.LocalName;
                if (elementName.Equals("add", StringComparison.OrdinalIgnoreCase))
                {
                    values.Add(ReadValue(element));
                }
                else if (elementName.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    values.Clear();
                }
            }
            return values.AsReadOnly();
        }

        private void Save()
        {
            _fileSystem.AddFile(_fileName, _config.Save);
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

        private static XElement GetSection(XElement parentElement, string section)
        {
            section = XmlConvert.EncodeLocalName(section);
            return parentElement.Element(section);
        }

        private static XElement GetOrCreateSection(XElement parentElement, string sectionName)
        {
            sectionName = XmlConvert.EncodeLocalName(sectionName);
            var section = parentElement.Element(sectionName);
            if (section == null)
            {
                section = new XElement(sectionName);
                parentElement.Add(section);
            }
            return section;
        }

        private static XElement FindElementByKey(XElement sectionElement, string key)
        {
            XElement result = null;
            foreach (var element in sectionElement.Elements())
            {
                string elementName = element.Name.LocalName;
                if (elementName.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    result = null;
                }
                else if (elementName.Equals("add", StringComparison.OrdinalIgnoreCase) && 
                         element.GetOptionalAttributeValue("key").Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    result = element;
                }
            }
            return result;
        }

        private static IList<KeyValuePair<string, string>> EmptyList()
        {
            return new KeyValuePair<string, string>[0];
        }
    }
}
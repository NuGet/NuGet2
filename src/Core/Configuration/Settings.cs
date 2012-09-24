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
        // next config file to read if any
        private Settings _next;

        public Settings(IFileSystem fileSystem)
            : this(fileSystem, Constants.SettingsFileName)
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
            get
            {
                return Path.IsPathRooted(_fileName) ?
                            _fileName : Path.GetFullPath(Path.Combine(_fileSystem.Root, _fileName));
            }
        }

        public static ISettings LoadDefaultSettings(IFileSystem fileSystem)
        {
            // Walk up the tree to find a config file; also look in .nuget subdirectories
            // Finally look in %APPDATA%\NuGet
            var validSettingFiles = new List<Settings>();
            if (fileSystem != null)
            {
                validSettingFiles.AddRange(
                    GetSettingsFileNames(fileSystem)
                        .Select(f => ReadSettings(fileSystem, f))
                        .Where(f => f != null));
            }

            // for the default location, allow case where file does not exist, in which case it'll end
            // up being created if needed
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!String.IsNullOrEmpty(appDataPath))
            {
                var defaultSettingsPath = Path.Combine(appDataPath, "NuGet");
                var appDataSettings = ReadSettings(new PhysicalFileSystem(defaultSettingsPath),
                                                   Constants.SettingsFileName);
                if (appDataSettings != null)
                {
                    validSettingFiles.Add(appDataSettings);
                }
            }

            if (validSettingFiles.IsEmpty())
            {
                // This means we've failed to load all config files and also failed to load or create the one in %AppData%
                // Work Item 1531: If the config file is malformed and the constructor throws, NuGet fails to load in VS. 
                // Returning a null instance prevents us from silently failing and also from picking up the wrong config
                return NullSettings.Instance;
            }

            // if multiple setting files were loaded, chain them in a linked list
            for (int i = 1; i < validSettingFiles.Count; ++i)
            {
                validSettingFiles[i]._next = validSettingFiles[i - 1];
            }

            // return the linked list head, typically %APPDATA%\NuGet\nuget.config
            // This is the one we want to read first, and also the one that we want to write to
            // TODO: add UI to allow specifying which one to write to
            return validSettingFiles.Last();
        }

        public string GetValue(string section, string key)
        {
            return GetValue(section, key, isPath: false);
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

            XElement element = null;
            string ret = null;

            var curr = this;
            while (curr != null)
            {
                XElement newElement = curr.GetValueInternal(section, key, element);
                if (!object.ReferenceEquals(element, newElement))
                {
                    element = newElement;

                    // we need to evaluate using current Settings in case value needs path transformation
                    ret = curr.ElementToValue(element, isPath);
                }
                curr = curr._next;
            }

            return ret;
        }

        private string ElementToValue(XElement element, bool isPath)
        {
            if (element == null)
            {
                return null;
            }

            // Return the optional value which if not there will be null;
            string value = element.GetOptionalAttributeValue("value");
            if (!isPath || String.IsNullOrEmpty(value))
            {
                return value;
            }
            // if value represents a path and relative to this file path was specified, 
            // append location of file
            string configDirectory = Path.GetDirectoryName(ConfigFilePath);
            return _fileSystem.GetFullPath(Path.Combine(configDirectory, value));
        }

        private XElement GetValueInternal(string section, string key, XElement curr)
        {
            // Get the section and return curr if it doesn't exist
            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return curr;
            }

            // Get the add element that matches the key and return curr if it doesn't exist
            return FindElementByKey(sectionElement, key, curr);
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var values = new List<KeyValuePair<string, string>>();
            var curr = this;
            while (curr != null)
            {
                curr.PopulateValues(section, values);
                curr = curr._next;
            }

            return values.AsReadOnly();
        }

        private void PopulateValues(string section, List<KeyValuePair<string, string>> current)
        {
            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement != null)
            {
                ReadSection(sectionElement, current);
            }
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

            var values = new List<KeyValuePair<string, string>>();
            var curr = this;
            while (curr != null)
            {
                curr.PopulateNestedValues(section, key, values);
                curr = curr._next;
            }

            return values.AsReadOnly();
        }

        private void PopulateNestedValues(string section, string key, List<KeyValuePair<string, string>> current)
        {
            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return;
            }
            var subSection = GetSection(sectionElement, key);
            if (subSection == null)
            {
                return;
            }
            ReadSection(subSection, current);
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

            var element = FindElementByKey(sectionElement, key, null);
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

            var elementToDelete = FindElementByKey(sectionElement, key, null);
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

        private void ReadSection(XContainer sectionElement, ICollection<KeyValuePair<string, string>> values)
        {
            var elements = sectionElement.Elements();

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

        private static XElement FindElementByKey(XElement sectionElement, string key, XElement curr)
        {
            XElement result = curr;
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
        
        /// <remarks>
        /// Order is most significant (e.g. applied last) to least significant (applied first)
        /// ex:
        /// c:\foo\nuget.config
        /// c:\nuget.config
        /// </remarksy>
        private static IEnumerable<string> GetSettingsFileNames(IFileSystem fileSystem)
        {
            // for dirs obtained by walking up the tree, only consider setting files that already exist.
            // otherwise we'd end up creating them.
            foreach (var dir in GetSettingsFilePaths(fileSystem))
            {
                string fileName = Path.Combine(dir, Constants.SettingsFileName);

                // This is to workaround limitations in the filesystem mock implementations that assume relative paths.
                // For example MockFileSystem.Paths is holding relative paths, which whould be responsible for hundreds
                // of failures should this code could go away
                if (fileName.StartsWith(fileSystem.Root, StringComparison.OrdinalIgnoreCase))
                {
                    int count = fileSystem.Root.Length;
                    // if fileSystem.Root ends with \ (ex: c:\foo\) then we've removed all we needed
                    // otherwise, remove one more char
                    if (!(fileSystem.Root.EndsWith("\\") || fileSystem.Root.EndsWith("/")))
                    {
                        count++;
                    }
                    fileName = fileName.Substring(count);
                }

                if (fileSystem.FileExists(fileName))
                {
                    yield return fileName;
                }
            }
        }

        private static IEnumerable<string> GetSettingsFilePaths(IFileSystem fileSystem)
        {
            string root = fileSystem.Root;
            while (root != null)
            {
                yield return root;
                root = Path.GetDirectoryName(root);
            }
        }

        private static Settings ReadSettings(IFileSystem fileSystem, string settingsPath)
        {
            try
            {
                return new Settings(fileSystem, settingsPath);
            }
            catch (XmlException)
            {
                return null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
        // The priority of this setting file
        private int _priority;
        private readonly bool _isMachineWideSettings;

        public Settings(IFileSystem fileSystem)
            : this(fileSystem, Constants.SettingsFileName, false)
        {
        }

        public Settings(IFileSystem fileSystem, string fileName)
            : this(fileSystem, fileName, false)
        {
        }

        public Settings(IFileSystem fileSystem, string fileName, bool isMachineWideSettings)
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
            XDocument conf = null;
            ExecuteSynchronized(() => conf = XmlUtility.GetOrCreateDocument("configuration", _fileSystem, _fileName));
            _config = conf;
            _isMachineWideSettings = isMachineWideSettings;
        }

        /// <summary>
        /// Flag indicating whether this file is a machine wide settings file. A machine wide settings
        /// file will not be modified.
        /// </summary>
        public bool IsMachineWideSettings
        {
            get { return _isMachineWideSettings; }
        }

        public string ConfigFilePath
        {
            get
            {
                return Path.IsPathRooted(_fileName) ?
                            _fileName : Path.GetFullPath(Path.Combine(_fileSystem.Root, _fileName));
            }
        }

        /// <summary>
        /// Loads user settings from the NuGet configuration files. The method walks the directory
        /// tree in <paramref name="fileSystem"/> up to its root, and reads each NuGet.config file
        /// it finds in the directories. It then reads the user specific settings,
        /// which is file <paramref name="configFileName"/>
        /// in <paramref name="fileSystem"/> if <paramref name="configFileName"/> is not null,
        /// If <paramref name="configFileName"/> is null, the user specific settings file is
        /// %AppData%\NuGet\NuGet.config.
        /// After that, the machine wide settings files are added.
        /// </summary>
        /// <remarks>
        /// For example, if <paramref name="fileSystem"/> is c:\dir1\dir2, <paramref name="configFileName"/> 
        /// is "userConfig.file", the files loaded are (in the order that they are loaded):
        ///     c:\dir1\dir2\nuget.config
        ///     c:\dir1\nuget.config
        ///     c:\nuget.config
        ///     c:\dir1\dir2\userConfig.file
        ///     machine wide settings (e.g. c:\programdata\NuGet\Config\*.config)
        /// </remarks>
        /// <param name="fileSystem">The file system to walk to find configuration files.</param>
        /// <param name="configFileName">The user specified configuration file.</param>
        /// <param name="machineWideSettings">The machine wide settings. If it's not null, the
        /// settings files in the machine wide settings are added after the user sepcific 
        /// config file.</param>
        /// <returns>The settings object loaded.</returns>
        public static ISettings LoadDefaultSettings(
            IFileSystem fileSystem,
            string configFileName,
            IMachineWideSettings machineWideSettings)
        {
            // Walk up the tree to find a config file; also look in .nuget subdirectories
            var validSettingFiles = new List<Settings>();
            if (fileSystem != null)
            {
                validSettingFiles.AddRange(
                    GetSettingsFileNames(fileSystem)
                        .Select(f => ReadSettings(fileSystem, f))
                        .Where(f => f != null));
            }

            LoadUserSpecificSettings(validSettingFiles, fileSystem, configFileName);

            if (machineWideSettings != null)
            {
                validSettingFiles.AddRange(
                    machineWideSettings.Settings.Select(
                        s => new Settings(s._fileSystem, s._fileName, s._isMachineWideSettings)));
            }

            if (validSettingFiles.IsEmpty())
            {
                // This means we've failed to load all config files and also failed to load or create the one in %AppData%
                // Work Item 1531: If the config file is malformed and the constructor throws, NuGet fails to load in VS. 
                // Returning a null instance prevents us from silently failing and also from picking up the wrong config
                return NullSettings.Instance;
            }

            validSettingFiles[0]._priority = validSettingFiles.Count;

            // if multiple setting files were loaded, chain them in a linked list
            for (int i = 1; i < validSettingFiles.Count; ++i)
            {
                validSettingFiles[i]._next = validSettingFiles[i - 1];
                validSettingFiles[i]._priority = validSettingFiles[i - 1]._priority - 1;
            }

            // return the linked list head. Typicall, it's either the config file in %ProgramData%\NuGet\Config,
            // or the user specific config (%APPDATA%\NuGet\nuget.config) if there are no machine
            // wide config files. The head file is the one we want to read first, while the user specific config 
            // is the one that we want to write to.
            // TODO: add UI to allow specifying which one to write to
            return validSettingFiles.Last();
        }

        private static void LoadUserSpecificSettings(
            List<Settings> validSettingFiles,
            IFileSystem fileSystem,
            string configFileName)
        {
            // for the default location, allow case where file does not exist, in which case it'll end
            // up being created if needed
            Settings appDataSettings = null;
            if (configFileName == null)
            {
                // load %AppData%\NuGet\NuGet.config
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!String.IsNullOrEmpty(appDataPath))
                {
                    var defaultSettingsFilePath = Path.Combine(appDataPath, "NuGet", Constants.SettingsFileName);
                    appDataSettings = ReadSettings(
                        fileSystem ?? new PhysicalFileSystem(Directory.GetCurrentDirectory()),
                        defaultSettingsFilePath);
                }
            }
            else
            {
                if (!fileSystem.FileExists(configFileName))
                {
                    string message = String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.FileDoesNotExit,
                        fileSystem.GetFullPath(configFileName));
                    throw new InvalidOperationException(message);
                }

                appDataSettings = ReadSettings(fileSystem, configFileName);
            }

            if (appDataSettings != null)
            {
                validSettingFiles.Add(appDataSettings);
            }
        }

        /// <summary>
        /// Loads the machine wide settings.
        /// </summary>
        /// <remarks>
        /// For example, if <paramref name="paths"/> is {"IDE", "Version", "SKU" }, then
        /// the files loaded are (in the order that they are loaded):
        ///     %programdata%\NuGet\Config\IDE\Version\SKU\*.config
        ///     %programdata%\NuGet\Config\IDE\Version\*.config
        ///     %programdata%\NuGet\Config\IDE\*.config
        ///     %programdata%\NuGet\Config\*.config
        /// </remarks>
        /// <param name="fileSystem">The file system in which the settings files are read.</param>
        /// <param name="paths">The additional paths under which to look for settings files.</param>
        /// <returns>The list of settings read.</returns>
        public static IEnumerable<Settings> LoadMachineWideSettings(
            IFileSystem fileSystem,
            params string[] paths)
        {
            List<Settings> settingFiles = new List<Settings>();
            string basePath = @"NuGet\Config";
            string combinedPath = Path.Combine(paths);

            while (true)
            {
                string directory = Path.Combine(basePath, combinedPath);

                // load setting files in directory
                foreach (var file in fileSystem.GetFiles(directory, "*.config"))
                {
                    var settings = ReadSettings(fileSystem, file, true);
                    if (settings != null)
                    {
                        settingFiles.Add(settings);
                    }
                }

                if (combinedPath.Length == 0)
                {
                    break;
                }

                int index = combinedPath.LastIndexOf(Path.DirectorySeparatorChar);
                if (index < 0)
                {
                    index = 0;
                }
                combinedPath = combinedPath.Substring(0, index);
            }

            return settingFiles;
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

        private static string ResolvePath(string configDirectory, string value)
        {
            // Three cases for when Path.IsRooted(value) is true:
            // 1- C:\folder\file
            // 2- \\share\folder\file
            // 3- \folder\file
            // In the first two cases, we want to honor the fully qualified path
            // In the last case, we want to return X:\folder\file with X: drive where config file is located.
            // However, Path.Combine(path1, path2) always returns path2 when Path.IsRooted(path2) == true (which is current case)
            var root = Path.GetPathRoot(value);
            // this corresponds to 3rd case
            if (root != null && root.Length == 1 && (root[0] == Path.DirectorySeparatorChar || value[0] == Path.AltDirectorySeparatorChar))
            {
                return Path.Combine(Path.GetPathRoot(configDirectory), value.Substring(1));
            }
            return Path.Combine(configDirectory, value);
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
            return _fileSystem.GetFullPath(ResolvePath(Path.GetDirectoryName(ConfigFilePath), value));
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

        public IList<SettingValue> GetValues(string section, bool isPath)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var settingValues = new List<SettingValue>();
            var curr = this;

            while (curr != null)
            {
                curr.PopulateValues(section, settingValues, isPath);
                curr = curr._next;
            }

            return settingValues.AsReadOnly();
        }

        private void PopulateValues(string section, List<SettingValue> current, bool isPath)
        {
            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement != null)
            {
                ReadSection(sectionElement, current, isPath);
            }
        }

        public IList<SettingValue> GetNestedValues(string section, string subsection)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(subsection))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "subsection");
            }

            var values = new List<SettingValue>();
            var curr = this;

            while (curr != null)
            {
                curr.PopulateNestedValues(section, subsection, values);
                curr = curr._next;
            }

            return values;
        }

        private void PopulateNestedValues(string section, string subsection, List<SettingValue> current)
        {
            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return;
            }
            var subsectionElement = GetSection(sectionElement, subsection);
            if (subsectionElement == null)
            {
                return;
            }
            ReadSection(subsectionElement, current, isPath: false);
        }

        public void SetValue(string section, string key, string value)
        {
            // machine wide settings cannot be changed.
            if (IsMachineWideSettings)
            {
                if (_next == null)
                {
                    throw new InvalidOperationException(NuGetResources.Error_NoWritableConfig);
                }

                _next.SetValue(section, key, value);
                return;
            }

            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            var sectionElement = GetOrCreateSection(_config.Root, section);
            SetValueInternal(sectionElement, key, value, attributes: null);
            Save();
        }

        public void SetValues(string section, IReadOnlyList<SettingValue> values)
        {
            // machine wide settings cannot be changed.
            if (IsMachineWideSettings)
            {
                if (_next == null)
                {
                    throw new InvalidOperationException(NuGetResources.Error_NoWritableConfig);
                }

                _next.SetValues(section, values);
                return;
            }

            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var sectionElement = GetOrCreateSection(_config.Root, section);
            foreach (var value in values)
            {
                SetValueInternal(sectionElement, value.Key, value.Value, value.AdditionalData);
            }
            Save();
        }

        public void UpdateSections(string section, IReadOnlyList<SettingValue> values)
        {
            // machine wide settings cannot be changed.
            if (IsMachineWideSettings)
            {
                if (_next == null)
                {
                    throw new InvalidOperationException(NuGetResources.Error_NoWritableConfig);
                }

                _next.UpdateSections(section, values);
                return;
            }

            if (string.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement != null)
            {
                sectionElement.RemoveIndented();
            }

            var valuesToWrite = _next == null ? values : values.Where(v => v.Priority < _next._priority);

            if (valuesToWrite.Any())
            {
                sectionElement = GetOrCreateSection(_config.Root, section);
            }

            foreach (var value in valuesToWrite)
            {
                var element = new XElement("add");
                SetElementValues(element, value.Key, value.Value, value.AdditionalData);
                sectionElement.AddIndented(element);
            }

            Save();

            if (_next != null)
            {
                _next.UpdateSections(section, values.Where(v => v.Priority >= _next._priority).ToList());
            }
        }

        private static void SetElementValues(XElement element, string key, string value, IDictionary<string, string> attributes)
        {
            foreach (var existingAttribute in element.Attributes())
            {
                if (!string.Equals(existingAttribute.Name.LocalName, "key", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existingAttribute.Name.LocalName, "value", StringComparison.OrdinalIgnoreCase) &&
                    !attributes.ContainsKey(existingAttribute.Name.LocalName))
                {
                    // Remove previously existing attributes that are no longer present.
                    existingAttribute.Remove();
                }
            }

            element.SetAttributeValue("key", key);
            element.SetAttributeValue("value", value);

            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    element.SetAttributeValue(attribute.Key, attribute.Value);
                }
            }
        }

        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
            // machine wide settings cannot be changed.
            if (IsMachineWideSettings)
            {
                if (_next == null)
                {
                    throw new InvalidOperationException(NuGetResources.Error_NoWritableConfig);
                }

                _next.SetNestedValues(section, key, values);
                return;
            }

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
                SetValueInternal(element, kvp.Key, kvp.Value, attributes: null);
            }
            Save();
        }


        private void SetValueInternal(XElement sectionElement, string key, string value, IDictionary<string, string> attributes)
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
                SetElementValues(element, key, value, attributes);
                Save();
            }
            else
            {
                element = new XElement("add");
                SetElementValues(element, key, value, attributes);
                sectionElement.AddIndented(element);
            }
        }

        public bool DeleteValue(string section, string key)
        {
            // machine wide settings cannot be changed.
            if (IsMachineWideSettings)
            {
                if (_next == null)
                {
                    throw new InvalidOperationException(NuGetResources.Error_NoWritableConfig);
                }

                return _next.DeleteValue(section, key);
            }

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
            elementToDelete.RemoveIndented();
            Save();
            return true;
        }

        public bool DeleteSection(string section)
        {
            // machine wide settings cannot be changed.
            if (IsMachineWideSettings)
            {
                if (_next == null)
                {
                    throw new InvalidOperationException(NuGetResources.Error_NoWritableConfig);
                }

                return _next.DeleteSection(section);
            }

            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            var sectionElement = GetSection(_config.Root, section);
            if (sectionElement == null)
            {
                return false;
            }

            sectionElement.RemoveIndented();
            Save();
            return true;
        }

        private void ReadSection(
            XContainer sectionElement,
            ICollection<SettingValue> values,
            bool isPath)
        {
            var elements = sectionElement.Elements();

            foreach (var element in elements)
            {
                string elementName = element.Name.LocalName;
                if (elementName.Equals("add", StringComparison.OrdinalIgnoreCase))
                {
                    values.Add(ReadSettingsValue(element, isPath));
                }
                else if (elementName.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    values.Clear();
                }
            }
        }

        private void Save()
        {
            ExecuteSynchronized(() => _fileSystem.AddFile(_fileName, _config.Save));
        }

        // When isPath is true, then the setting value is checked to see if it can be interpreted
        // as relative path. If it can, the returned value will be the full path of the relative path.
        // If it cannot be interpreted as relative path, the value is returned as-is.
        private SettingValue ReadSettingsValue(XElement element, bool isPath)
        {
            var keyAttribute = element.Attribute("key");
            var valueAttribute = element.Attribute("value");

            if (keyAttribute == null || String.IsNullOrEmpty(keyAttribute.Value) || valueAttribute == null)
            {
                throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, NuGetResources.UserSettings_UnableToParseConfigFile, ConfigFilePath));
            }

            var value = valueAttribute.Value;
            Uri uri;
            if (isPath && Uri.TryCreate(value, UriKind.Relative, out uri))
            {
                string configDirectory = Path.GetDirectoryName(ConfigFilePath);
                value = _fileSystem.GetFullPath(Path.Combine(configDirectory, value));
            }

            var settingValue = new SettingValue(keyAttribute.Value, value, IsMachineWideSettings, _priority);
            foreach (var attribute in element.Attributes())
            {
                // Add all attributes other than ConfigurationContants.KeyAttribute and ConfigurationContants.ValueAttribute to AdditionalValues
                if (!string.Equals(attribute.Name.LocalName, "key", StringComparison.Ordinal) &&
                    !string.Equals(attribute.Name.LocalName, "value", StringComparison.Ordinal))
                {
                    settingValue.AdditionalData[attribute.Name.LocalName] = attribute.Value;
                }
            }

            return settingValue;
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
                parentElement.AddIndented(section);
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
        /// </remarks>
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
            return ReadSettings(fileSystem, settingsPath, false);
        }

        private static Settings ReadSettings(IFileSystem fileSystem, string settingsPath, bool isMachineWideSettings)
        {
            try
            {
                return new Settings(fileSystem, settingsPath, isMachineWideSettings);
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// Wrap all IO operations on setting files with this function to avoid file-in-use errors
        /// </summary>
        private void ExecuteSynchronized(Action ioOperation)
        {
            var fileName = _fileSystem.GetFullPath(_fileName);

            // Global: ensure mutex is honored across TS sessions 
            using (var mutex = new Mutex(false, "Global\\" + EncryptionUtility.GenerateUniqueToken(fileName)))
            {
                var owner = false;
                try
                {
                    // operations on NuGet.config should be very short lived
                    owner = mutex.WaitOne(TimeSpan.FromMinutes(1));
                    // decision here is to proceed even if we were not able to get mutex ownership
                    // and let the potential IO errors bubble up. Reasoning is that failure to get
                    // ownership probably means faulty hardware and in this case it's better to report
                    // back than hang
                    ioOperation();
                }
                finally
                {
                    if (owner)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

        }
    }
}
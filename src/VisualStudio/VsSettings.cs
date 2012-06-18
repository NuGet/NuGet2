using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace NuGet.VisualStudio
{
    [Export(typeof(ISettings))]
    public class VsSettings : ISettings
    {
        private const string SolutionConfigSection = "solution";
        public static readonly string SourceControlSupportKey = "disableSourceControlIntegration";
        private readonly ISolutionManager _solutionManager;
        private readonly ISettings _defaultSettings;
        private readonly IFileSystemProvider _fileSystemProvider;

        [ImportingConstructor]
        public VsSettings(ISolutionManager solutionManager)
            : this(solutionManager, 
            Settings.LoadDefaultSettings(null == solutionManager ? null : solutionManager.SolutionFileSystem), 
            new PhysicalFileSystemProvider())
        {
            // Review: Do we need to pass in the VsFileSystemProvider here instead of hardcoding PhysicalFileSystems?
        }

        public VsSettings(ISolutionManager solutionManager, ISettings defaultSettings, IFileSystemProvider fileSystemProvider)
        {
            if (solutionManager == null)
            {
                throw new ArgumentNullException("solutionManager");
            }
            if (defaultSettings == null)
            {
                throw new ArgumentNullException("defaultSettings");
            }
            if (fileSystemProvider == null)
            {
                throw new ArgumentNullException("fileSystemProvider");
            }

            _solutionManager = solutionManager;
            _defaultSettings = defaultSettings;
            _fileSystemProvider = fileSystemProvider;
        }

        private ISettings SolutionSettings
        {
            get
            {
                if (_solutionManager.IsSolutionOpen && !String.IsNullOrEmpty(_solutionManager.SolutionDirectory))
                {
                    var nugetSettingsDirectory = Path.Combine(_solutionManager.SolutionDirectory, VsConstants.NuGetSolutionSettingsFolder);
                    var fileSystem = _fileSystemProvider.GetFileSystem(nugetSettingsDirectory);

                    if (fileSystem.FileExists(Constants.SettingsFileName))
                    {
                        return new Settings(fileSystem);
                    }
                }
                return NullSettings.Instance;
            }
        }

        public string GetValue(string section, string key)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                return SolutionSettings.GetValue(section, key);
            }
            return _defaultSettings.GetValue(section, key);
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                return SolutionSettings.GetValues(section);
            }
            return _defaultSettings.GetValues(section);
        }

        public IList<KeyValuePair<string, string>> GetNestedValues(string section, string key)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                return SolutionSettings.GetNestedValues(section, key);
            }
            return _defaultSettings.GetNestedValues(section, key);
        }

        public void SetValue(string section, string key, string value)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                SolutionSettings.SetValue(section, key, value);
            }
            else
            {
                _defaultSettings.SetValue(section, key, value);
            }
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                SolutionSettings.SetValues(section, values);
            }
            else
            {
                _defaultSettings.SetValues(section, values);
            }
        }

        public void SetNestedValues(string section, string key, IList<KeyValuePair<string, string>> values)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                SolutionSettings.SetNestedValues(section, key, values);
            }
            else
            {
                _defaultSettings.SetNestedValues(section, key, values);
            }
        }

        public bool DeleteValue(string section, string key)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                return SolutionSettings.DeleteValue(section, key);
            }
            return _defaultSettings.DeleteValue(section, key);
        }

        public bool DeleteSection(string section)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                return SolutionSettings.DeleteSection(section);
            }
            return _defaultSettings.DeleteSection(section);
        }

        private sealed class PhysicalFileSystemProvider : IFileSystemProvider
        {
            public IFileSystem GetFileSystem(string path)
            {
                return new PhysicalFileSystem(path);
            }
        }
    }
}

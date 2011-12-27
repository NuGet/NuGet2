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
        private ISettings _solutionSettings;

        [ImportingConstructor]
        public VsSettings(ISolutionManager solutionManager)
            : this(solutionManager, Settings.LoadDefaultSettings(), new PhysicalFileSystemProvider())
        {
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

            EventHandler eventHandler = (src, eventArgs) =>
            {
                _solutionSettings = null;
            };
            _solutionManager.SolutionOpened += eventHandler;
            _solutionManager.SolutionClosed += eventHandler;
        }

        public string GetValue(string section, string key)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                EnsureSolutionSettings();
                return _solutionSettings.GetValue(section, key);
            }
            return _defaultSettings.GetValue(section, key);
        }

        public IList<KeyValuePair<string, string>> GetValues(string section)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                EnsureSolutionSettings();
                return _solutionSettings.GetValues(section);
            }
            return _defaultSettings.GetValues(section);
        }

        public void SetValue(string section, string key, string value)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                EnsureSolutionSettings();
                _solutionSettings.SetValue(section, key, value);
            }
            _defaultSettings.SetValue(section, key, value);
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                EnsureSolutionSettings();
                _solutionSettings.SetValues(section, values);
            }
            _defaultSettings.SetValues(section, values);
        }

        public bool DeleteValue(string section, string key)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                EnsureSolutionSettings();
                return _solutionSettings.DeleteValue(section, key);
            }
            return _defaultSettings.DeleteValue(section, key);
        }

        public bool DeleteSection(string section)
        {
            if (section.Equals(SolutionConfigSection, StringComparison.OrdinalIgnoreCase))
            {
                EnsureSolutionSettings();
                return _solutionSettings.DeleteSection(section);
            }
            return _defaultSettings.DeleteSection(section);
        }

        private void EnsureSolutionSettings()
        {
            if (_solutionManager.IsSolutionOpen && !String.IsNullOrEmpty(_solutionManager.SolutionDirectory))
            {
                if (_solutionSettings != null)
                {
                    // We already have a cached config in memory. Do nothing.
                    return;
                }

                var nugetSettingsDirectory = Path.Combine(_solutionManager.SolutionDirectory, VsConstants.NuGetSolutionSettingsFolder);
                var fileSystem = _fileSystemProvider.GetFileSystem(nugetSettingsDirectory);
                if (fileSystem.FileExists(Constants.SettingsFileName))
                {
                    _solutionSettings = new Settings(fileSystem);
                    return;
                }
            }
            _solutionSettings = NullSettings.Instance;
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

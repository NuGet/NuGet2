using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {

    [Export(typeof(ISettingsManager))]
    public class SettingsManager : ISettingsManager {

        public const string ApiKeysSectionName = "apikeys";

        public IList<string> GetMruFiles() {
            var files = Properties.Settings.Default.MruFiles;
            return files == null ? new List<string>() : files.Cast<string>().ToList();
        }

        public void SetMruFiles(IEnumerable<string> files) {
            StringCollection sc = new StringCollection();
            sc.AddRange(files.ToArray());
            Properties.Settings.Default.MruFiles = sc;
        }

        public string ReadApiKeyFromSettingFile() {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            string key = settings.GetDecryptedValue(ApiKeysSectionName, GalleryServer.DefaultGalleryServerUrl);
            return key ?? Properties.Settings.Default.PublishPrivateKey;
        }

        public void WriteApiKeyToSettingFile(string apiKey) {
            var settings = new UserSettings(new PhysicalFileSystem(Environment.CurrentDirectory));
            settings.SetEncryptedValue(ApiKeysSectionName, GalleryServer.DefaultGalleryServerUrl, apiKey);
        }
    }
}